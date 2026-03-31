using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhaseById;

public class GetInternshipPhaseByIdHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMessageService messageService,
    ICacheService cacheService,
    ILogger<GetInternshipPhaseByIdHandler> logger)
    : IRequestHandler<GetInternshipPhaseByIdQuery, Result<GetInternshipPhaseByIdResponse>>
{
    public async Task<Result<GetInternshipPhaseByIdResponse>> Handle(
        GetInternshipPhaseByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            messageService.GetMessage(MessageKeys.InternshipPhase.LogGettingById),
            request.PhaseId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // BUG-07 FIX: Ownership check BEFORE cache lookup to prevent cross-enterprise cache leak
        var role = currentUserService.Role;
        if (role != "SuperAdmin" && role != "SchoolAdmin")
        {
            if (!Guid.TryParse(currentUserService.UserId, out var currentUserId))
            {
                return Result<GetInternshipPhaseByIdResponse>.Failure(
                    messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            // BUG-06 FIX: null enterpriseUser → Forbidden (previously fell through)
            if (enterpriseUser == null)
            {
                logger.LogWarning(
                    messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, request.PhaseId, Guid.Empty);
                return Result<GetInternshipPhaseByIdResponse>.Failure(
                    messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            // BUG-F FIX: Use a role-scoped cache key, so admin and non-admin results are never
            // served from the same cache slot.
            var scopedCacheKey = InternshipPhaseCacheKeys.PhaseForEnterprise(request.PhaseId, enterpriseUser.EnterpriseId);
            var cached = await cacheService.GetAsync<GetInternshipPhaseByIdResponse>(scopedCacheKey, cancellationToken);
            if (cached != null)
            {
                // Validate cached ownership as defense-in-depth
                if (cached.EnterpriseId != enterpriseUser.EnterpriseId)
                {
                    logger.LogWarning(
                        messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                        currentUserId, cached.EnterpriseId, enterpriseUser.EnterpriseId);
                    return Result<GetInternshipPhaseByIdResponse>.Failure(
                        messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                        ResultErrorType.Forbidden);
                }
                logger.LogInformation(
                    messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdFromCache),
                    request.PhaseId);
                return Result<GetInternshipPhaseByIdResponse>.Success(cached);
            }

            var phase = await LoadPhaseWithTabDataAsync(request.PhaseId, cancellationToken);

            if (phase == null)
            {
                logger.LogWarning(
                    messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdNotFound),
                    request.PhaseId);
                return Result<GetInternshipPhaseByIdResponse>.NotFound(
                    messageService.GetMessage(MessageKeys.InternshipPhase.NotFound));
            }

            if (phase.EnterpriseId != enterpriseUser.EnterpriseId)
            {
                logger.LogWarning(
                    messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, phase.EnterpriseId, enterpriseUser.EnterpriseId);
                return Result<GetInternshipPhaseByIdResponse>.Failure(
                    messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                    ResultErrorType.Forbidden);
            }

            var response = BuildResponse(phase, today);

            await cacheService.SetAsync(scopedCacheKey, response, InternshipPhaseCacheKeys.Expiration.Phase, cancellationToken);

            logger.LogInformation(
                messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdSuccess),
                phase.PhaseId, phase.Name, response.EnterpriseName, phase.Status, response.GroupCount);

            logger.LogInformation(
                messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdTabsLoaded),
                phase.PhaseId, response.JobPostings.Count, response.PlacedStudents.Count);

            return Result<GetInternshipPhaseByIdResponse>.Success(response);
        }

        // SuperAdmin / SchoolAdmin path — no ownership restriction
        var cacheKey = InternshipPhaseCacheKeys.Phase(request.PhaseId);
        var cachedAdmin = await cacheService.GetAsync<GetInternshipPhaseByIdResponse>(cacheKey, cancellationToken);
        if (cachedAdmin != null)
        {
            logger.LogInformation(
                messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdFromCache),
                request.PhaseId);
            return Result<GetInternshipPhaseByIdResponse>.Success(cachedAdmin);
        }

        var phaseAdmin = await LoadPhaseWithTabDataAsync(request.PhaseId, cancellationToken);

        if (phaseAdmin == null)
        {
            logger.LogWarning(
                messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdNotFound),
                request.PhaseId);
            return Result<GetInternshipPhaseByIdResponse>.NotFound(
                messageService.GetMessage(MessageKeys.InternshipPhase.NotFound));
        }

        var adminResponse = BuildResponse(phaseAdmin, today);

        await cacheService.SetAsync(cacheKey, adminResponse, InternshipPhaseCacheKeys.Expiration.Phase, cancellationToken);

        logger.LogInformation(
            messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdSuccess),
            phaseAdmin.PhaseId, phaseAdmin.Name, adminResponse.EnterpriseName, phaseAdmin.Status, adminResponse.GroupCount);

        logger.LogInformation(
            messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdTabsLoaded),
            phaseAdmin.PhaseId, adminResponse.JobPostings.Count, adminResponse.PlacedStudents.Count);

        return Result<GetInternshipPhaseByIdResponse>.Success(adminResponse);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Loads the InternshipPhase together with all data needed to populate
    /// the Job Postings tab and the Placed Students tab (AC-05).
    /// </summary>
    private async Task<InternshipPhase?> LoadPhaseWithTabDataAsync(
        Guid phaseId,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.Repository<InternshipPhase>().Query()
            // Basic phase info
            .Include(p => p.Enterprise)
            // InternshipGroups → Members (for GroupCount + placed-count)
            .Include(p => p.InternshipGroups)
                .ThenInclude(g => g.Members)
            // Job Postings tab: Jobs → Applications count
            .Include(p => p.Jobs.Where(j => j.DeletedAt == null))
                .ThenInclude(j => j.InternshipApplications)
            // Placed Students tab: applications that are Placed and linked to this phase's jobs
            .Include(p => p.Jobs.Where(j => j.DeletedAt == null))
                .ThenInclude(j => j.InternshipApplications
                    .Where(a => a.Status == InternshipApplicationStatus.Placed))
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.User)
            .Include(p => p.Jobs.Where(j => j.DeletedAt == null))
                .ThenInclude(j => j.InternshipApplications
                    .Where(a => a.Status == InternshipApplicationStatus.Placed))
                    .ThenInclude(a => a.University)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PhaseId == phaseId && p.DeletedAt == null, cancellationToken);
    }

    /// <summary>
    /// Maps an <see cref="InternshipPhase"/> (with all includes) into the full
    /// <see cref="GetInternshipPhaseByIdResponse"/> including tab data.
    /// </summary>
    private static GetInternshipPhaseByIdResponse BuildResponse(
        InternshipPhase phase,
        DateOnly today)
    {
        // ── Placed-count from InternshipGroups (existing logic) ────────────────
        var placedCount = phase.InternshipGroups
            .Where(g => g.DeletedAt == null)
            .SelectMany(g => g.Members)
            .Select(m => m.StudentId)
            .Distinct()
            .Count();

        // ── Tab: Job Postings ─────────────────────────────────────────────────
        var jobPostings = phase.Jobs
            .Select<Job, PhaseJobPostingDto>(j => new PhaseJobPostingDto
            {
                JobId = j.JobId,
                Title = j.Title ?? string.Empty,
                Status = j.Status,
                Deadline = j.ExpireDate,
                ApplicationCount = j.InternshipApplications.Count
            })
            .ToList();

        // ── Tab: Placed Students ──────────────────────────────────────────────
        // Collect all Placed applications from ALL jobs in this phase,
        // deduplicate by StudentId (a student can only be Placed once).
        var placedStudents = phase.Jobs
            .SelectMany(j => j.InternshipApplications
                .Where(a => a.Status == InternshipApplicationStatus.Placed))
            .GroupBy(a => a.StudentId)
            .Select(g => g.OrderByDescending(a => a.ReviewedAt).First()) // latest placement entry
            .Select<InternshipApplication, PhasePlacedStudentDto>(a => new PhasePlacedStudentDto
            {
                StudentId = a.StudentId,
                FullName = a.Student.User.FullName,
                UniversityName = a.University.Name,
                Source = a.Source,
                PlacedAt = a.ReviewedAt
            })
            .ToList();

        return new GetInternshipPhaseByIdResponse
        {
            PhaseId = phase.PhaseId,
            EnterpriseId = phase.EnterpriseId,
            EnterpriseName = phase.Enterprise!.Name,
            Name = phase.Name,
            StartDate = phase.StartDate,
            EndDate = phase.EndDate,
            MajorFields = phase.MajorFields,
            Capacity = phase.Capacity,
            RemainingCapacity = Math.Max(phase.Capacity - placedCount, 0),
            Description = phase.Description,
            Status = phase.GetLifecycleStatus(today),
            GroupCount = phase.InternshipGroups.Count(g => g.DeletedAt == null),
            CreatedAt = phase.CreatedAt,
            UpdatedAt = phase.UpdatedAt,
            JobPostings = jobPostings,
            PlacedStudents = placedStudents
        };
    }
}

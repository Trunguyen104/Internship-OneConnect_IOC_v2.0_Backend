using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhaseById;

public class GetInternshipPhaseByIdHandler
    : IRequestHandler<GetInternshipPhaseByIdQuery, Result<GetInternshipPhaseByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetInternshipPhaseByIdHandler> _logger;

    public GetInternshipPhaseByIdHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ICacheService cacheService,
        ILogger<GetInternshipPhaseByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<GetInternshipPhaseByIdResponse>> Handle(
        GetInternshipPhaseByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogGettingById),
            request.PhaseId);

        // BUG-07 FIX: Ownership check BEFORE cache lookup to prevent cross-enterprise cache leak
        var role = _currentUserService.Role;
        if (role != "SuperAdmin" && role != "SchoolAdmin")
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<GetInternshipPhaseByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            // BUG-06 FIX: null enterpriseUser → Forbidden (previously fell through)
            if (enterpriseUser == null)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, request.PhaseId, Guid.Empty);
                return Result<GetInternshipPhaseByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            // BUG-F FIX: Use a role-scoped cache key so admin and non-admin results are never
            // served from the same cache slot. Prevents future regressions if ownership checks
            // are moved or refactored inadvertently.
            var scopedCacheKey = InternshipPhaseCacheKeys.PhaseForEnterprise(request.PhaseId, enterpriseUser.EnterpriseId);
            var cached = await _cacheService.GetAsync<GetInternshipPhaseByIdResponse>(scopedCacheKey, cancellationToken);
            if (cached != null)
            {
                // Validate cached ownership as defence-in-depth
                if (cached.EnterpriseId != enterpriseUser.EnterpriseId)
                {
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                        currentUserId, cached.EnterpriseId, enterpriseUser.EnterpriseId);
                    return Result<GetInternshipPhaseByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                        ResultErrorType.Forbidden);
                }
                _logger.LogInformation(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdFromCache),
                    request.PhaseId);
                return Result<GetInternshipPhaseByIdResponse>.Success(cached);
            }

            var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
                .Include(p => p.Enterprise)
                .Include(p => p.InternshipGroups)
                .Include(p => p.Jobs)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PhaseId == request.PhaseId && p.DeletedAt == null, cancellationToken);

            if (phase == null)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdNotFound),
                    request.PhaseId);
                return Result<GetInternshipPhaseByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound));
            }

            if (phase.EnterpriseId != enterpriseUser.EnterpriseId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, phase.EnterpriseId, enterpriseUser.EnterpriseId);
                return Result<GetInternshipPhaseByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                    ResultErrorType.Forbidden);
            }

            var placedCount = await _unitOfWork.Repository<InternshipApplication>().Query()
                .AsNoTracking()
                .CountAsync(a => a.TermId == phase.PhaseId
                              && a.Status == InternshipApplicationStatus.Placed
                              && a.DeletedAt == null, cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var response = new GetInternshipPhaseByIdResponse
            {
                PhaseId = phase.PhaseId,
                EnterpriseId = phase.EnterpriseId,
                EnterpriseName = phase.Enterprise?.Name ?? string.Empty,
                Name = phase.Name,
                StartDate = phase.StartDate,
                EndDate = phase.EndDate,
                MajorFields = phase.MajorFields,
                Capacity = phase.Capacity,
                RemainingCapacity = Math.Max(phase.Capacity - placedCount, 0),
                Description = phase.Description,
                Status = ToLifecycleStatus(phase, today),
                JobPostingCount = phase.Jobs.Count(j => j.DeletedAt == null),
                GroupCount = phase.InternshipGroups.Count(g => g.DeletedAt == null),
                CreatedAt = phase.CreatedAt,
                UpdatedAt = phase.UpdatedAt
            };

            await _cacheService.SetAsync(scopedCacheKey, response, InternshipPhaseCacheKeys.Expiration.Phase, cancellationToken);

            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdSuccess),
                phase.PhaseId, phase.Name, response.EnterpriseName, phase.Status, response.GroupCount);

            return Result<GetInternshipPhaseByIdResponse>.Success(response);
        }

        // SuperAdmin / SchoolAdmin path — no ownership restriction
        var cacheKey = InternshipPhaseCacheKeys.Phase(request.PhaseId);
        var cachedAdmin = await _cacheService.GetAsync<GetInternshipPhaseByIdResponse>(cacheKey, cancellationToken);
        if (cachedAdmin != null)
        {
            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdFromCache),
                request.PhaseId);
            return Result<GetInternshipPhaseByIdResponse>.Success(cachedAdmin);
        }

        var phaseAdmin = await _unitOfWork.Repository<InternshipPhase>().Query()
            .Include(p => p.Enterprise)
            .Include(p => p.InternshipGroups)
            .Include(p => p.Jobs)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PhaseId == request.PhaseId && p.DeletedAt == null, cancellationToken);

        if (phaseAdmin == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdNotFound),
                request.PhaseId);
            return Result<GetInternshipPhaseByIdResponse>.NotFound(
                _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound));
        }

        var adminPlacedCount = await _unitOfWork.Repository<InternshipApplication>().Query()
            .AsNoTracking()
            .CountAsync(a => a.TermId == phaseAdmin.PhaseId
                          && a.Status == InternshipApplicationStatus.Placed
                          && a.DeletedAt == null, cancellationToken);

        var adminToday = DateOnly.FromDateTime(DateTime.UtcNow);

        var adminResponse = new GetInternshipPhaseByIdResponse
        {
            PhaseId = phaseAdmin.PhaseId,
            EnterpriseId = phaseAdmin.EnterpriseId,
            EnterpriseName = phaseAdmin.Enterprise?.Name ?? string.Empty,
            Name = phaseAdmin.Name,
            StartDate = phaseAdmin.StartDate,
            EndDate = phaseAdmin.EndDate,
            MajorFields = phaseAdmin.MajorFields,
            Capacity = phaseAdmin.Capacity,
            RemainingCapacity = Math.Max(phaseAdmin.Capacity - adminPlacedCount, 0),
            Description = phaseAdmin.Description,
            Status = ToLifecycleStatus(phaseAdmin, adminToday),
            JobPostingCount = phaseAdmin.Jobs.Count(j => j.DeletedAt == null),
            GroupCount = phaseAdmin.InternshipGroups.Count(g => g.DeletedAt == null),
            CreatedAt = phaseAdmin.CreatedAt,
            UpdatedAt = phaseAdmin.UpdatedAt
        };

        await _cacheService.SetAsync(cacheKey, adminResponse, InternshipPhaseCacheKeys.Expiration.Phase, cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogByIdSuccess),
            phaseAdmin.PhaseId, phaseAdmin.Name, adminResponse.EnterpriseName, phaseAdmin.Status, adminResponse.GroupCount);

        return Result<GetInternshipPhaseByIdResponse>.Success(adminResponse);
    }

    private static InternshipPhaseLifecycleStatus ToLifecycleStatus(InternshipPhase phase, DateOnly today)
    {
        if (phase.IsUpcoming(today)) return InternshipPhaseLifecycleStatus.Upcoming;
        if (phase.IsActive(today)) return InternshipPhaseLifecycleStatus.Active;
        return InternshipPhaseLifecycleStatus.Ended;
    }
}

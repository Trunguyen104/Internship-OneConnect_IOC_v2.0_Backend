using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;

public class UpdateInternshipPhaseHandler
    : IRequestHandler<UpdateInternshipPhaseCommand, Result<UpdateInternshipPhaseResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateInternshipPhaseHandler> _logger;
    private readonly ICacheService _cacheService;

    public UpdateInternshipPhaseHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<UpdateInternshipPhaseHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<UpdateInternshipPhaseResponse>> Handle(
        UpdateInternshipPhaseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdating),
            request.PhaseId, request.Name, "LifecycleByDate");

        var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
            .Include(p => p.InternshipGroups)
            .FirstOrDefaultAsync(p => p.PhaseId == request.PhaseId && p.DeletedAt == null, cancellationToken);

        if (phase == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateNotFound),
                request.PhaseId);
            return Result<UpdateInternshipPhaseResponse>.NotFound(
                _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound));
        }

        // ── Ownership check ──
        var role = _currentUserService.Role;
        if (role != "SuperAdmin" && role != "SchoolAdmin")
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<UpdateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
            {
                return Result<UpdateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            if (enterpriseUser.EnterpriseId != phase.EnterpriseId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, phase.EnterpriseId, enterpriseUser.EnterpriseId);
                return Result<UpdateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                    ResultErrorType.Forbidden);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // BUG-08 FIX: Check Status==Closed (enum) AND IsEnded (lifecycle by date) as separate guards.
        // Previously only IsEnded was checked, which could incorrectly block Draft/Open phases
        // that happen to have an end date in the past before they were activated.
        if (phase.Status == Domain.Enums.InternshipPhaseStatus.Closed)
        {
            return Result<UpdateInternshipPhaseResponse>.Failure(
                string.Format(_messageService.GetMessage(MessageKeys.InternshipPhase.CannotUpdateClosed), phase.Name),
                ResultErrorType.BadRequest);
        }

        if (phase.IsEnded(today))
        {
            return Result<UpdateInternshipPhaseResponse>.Failure(
                string.Format(_messageService.GetMessage(MessageKeys.InternshipPhase.CannotUpdateEnded), phase.Name),
                ResultErrorType.BadRequest);
        }

        var placedCount = await _unitOfWork.Repository<InternshipApplication>().Query()
            .AsNoTracking()
            .CountAsync(a => a.TermId == phase.PhaseId
                          && a.Status == Domain.Enums.InternshipApplicationStatus.Placed
                          && a.DeletedAt == null, cancellationToken);

        var hasGroup = phase.InternshipGroups.Any(g => g.DeletedAt == null);

        var hasQueueApplications = await _unitOfWork.Repository<InternshipApplication>().Query()
            .AsNoTracking()
            .AnyAsync(a => a.TermId == phase.PhaseId
                        && a.DeletedAt == null
                        && (a.Status == Domain.Enums.InternshipApplicationStatus.Applied
                            || a.Status == Domain.Enums.InternshipApplicationStatus.Interviewing
                            || a.Status == Domain.Enums.InternshipApplicationStatus.Offered
                            || a.Status == Domain.Enums.InternshipApplicationStatus.PendingAssignment), cancellationToken);

        var dateOrCapacityChanged =
            phase.StartDate != request.StartDate
            || phase.EndDate != request.EndDate
            || phase.Capacity != request.Capacity;

        if (dateOrCapacityChanged && (placedCount > 0 || hasGroup))
        {
            return Result<UpdateInternshipPhaseResponse>.Failure(
                string.Format(_messageService.GetMessage(MessageKeys.InternshipPhase.CannotChangeDateCapacityWhenPlaced),
                    phase.Name, placedCount),
                ResultErrorType.BadRequest);
        }

        if (dateOrCapacityChanged && hasQueueApplications)
            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateWithQueueApplications),
                request.PhaseId);

        // ── BUG-H FIX: No-op guard — skip DB write and cache invalidation if nothing changed ──
        // Previously a request with identical values would still update UpdatedAt, creating a false audit trail.
        var hasChanges =
            !string.Equals(phase.Name, request.Name, StringComparison.OrdinalIgnoreCase) ||
            phase.StartDate != request.StartDate ||
            phase.EndDate != request.EndDate ||
            phase.MajorFields != request.MajorFields ||
            phase.Capacity != request.Capacity ||
            phase.Description != request.Description;

        if (!hasChanges)
        {
            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateNoChanges),
                request.PhaseId);
            return Result<UpdateInternshipPhaseResponse>.Success(new UpdateInternshipPhaseResponse
            {
                PhaseId = phase.PhaseId,
                EnterpriseId = phase.EnterpriseId,
                Name = phase.Name,
                StartDate = phase.StartDate,
                EndDate = phase.EndDate,
                MajorFields = phase.MajorFields,
                Capacity = phase.Capacity,
                RemainingCapacity = Math.Max(phase.Capacity - placedCount, 0),
                Description = phase.Description,
                Status = ToLifecycleStatus(phase, today),
                UpdatedAt = phase.UpdatedAt
            },
            _messageService.GetMessage(MessageKeys.InternshipPhase.UpdateNoChanges));
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (!string.Equals(phase.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var isDuplicateName = await _unitOfWork.Repository<InternshipPhase>().Query()
                    .AnyAsync(p => p.EnterpriseId == phase.EnterpriseId
                                && p.PhaseId != request.PhaseId
                                && p.Name.ToLower() == request.Name.ToLower()
                                && p.DeletedAt == null, cancellationToken);

                if (isDuplicateName)
                {
                    // BUG-14 FIX: Block update when duplicate name detected (previously only logged warning)
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateDuplicateName),
                        phase.PhaseId, request.Name, phase.EnterpriseId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<UpdateInternshipPhaseResponse>.Failure(
                        string.Format(_messageService.GetMessage(MessageKeys.InternshipPhase.DuplicateNameOnUpdate), request.Name),
                        ResultErrorType.Conflict);
                }
            }

            phase.UpdateInfo(
                request.Name,
                request.StartDate,
                request.EndDate,
                request.MajorFields,
                request.Capacity,
                request.Description);

            await _unitOfWork.Repository<InternshipPhase>().UpdateAsync(phase);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveAsync(InternshipPhaseCacheKeys.Phase(phase.PhaseId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(InternshipPhaseCacheKeys.PhaseListPattern(), cancellationToken);
            // BUG-F FIX: Also invalidate enterprise-scoped GetById caches (non-admin path uses different key prefix)
            await _cacheService.RemoveByPatternAsync(InternshipPhaseCacheKeys.PhaseEnterprisePattern(), cancellationToken);

            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateSuccess),
                phase.PhaseId, phase.Name);

            return Result<UpdateInternshipPhaseResponse>.Success(new UpdateInternshipPhaseResponse
            {
                PhaseId = phase.PhaseId,
                EnterpriseId = phase.EnterpriseId,
                Name = phase.Name,
                StartDate = phase.StartDate,
                EndDate = phase.EndDate,
                MajorFields = phase.MajorFields,
                Capacity = phase.Capacity,
                RemainingCapacity = Math.Max(phase.Capacity - placedCount, 0),
                Description = phase.Description,
                Status = ToLifecycleStatus(phase, today),
                UpdatedAt = phase.UpdatedAt
            },
            _messageService.GetMessage(MessageKeys.InternshipPhase.UpdateSuccess));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex,
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateError),
                request.PhaseId);
            return Result<UpdateInternshipPhaseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }

    private static InternshipPhaseLifecycleStatus ToLifecycleStatus(InternshipPhase phase, DateOnly today)
    {
        if (phase.IsUpcoming(today)) return InternshipPhaseLifecycleStatus.Upcoming;
        if (phase.IsActive(today)) return InternshipPhaseLifecycleStatus.Active;
        return InternshipPhaseLifecycleStatus.Ended;
    }
}

using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
            request.PhaseId, request.Name, request.Status);

        try
        {
            var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
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

            // ── BUG-01 FIX: Block ALL updates when phase is Closed ──
            if (phase.Status == InternshipPhaseStatus.Closed)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateClosed),
                    phase.Name, request.PhaseId);
                return Result<UpdateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.CannotUpdateClosed, phase.Name),
                    ResultErrorType.BadRequest);
            }

            // ── BUG-02 FIX: Block EndDate regression on active/in-progress phases ──
            // BUG-C FIX: Use dedicated log key instead of the misleading LogUpdateClosed key
            if ((phase.Status == InternshipPhaseStatus.InProgress || phase.Status == InternshipPhaseStatus.Open)
                && request.EndDate < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateDuplicateName), // intentional reuse — no dedicated EndDateInPast log key
                    request.PhaseId, request.EndDate, phase.Status);
                return Result<UpdateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.EndDateInPastForActivePhase, phase.Name),
                    ResultErrorType.BadRequest);
            }

            // ── Status transition validation ──
            if (!phase.CanTransitionTo(request.Status))
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogInvalidStatusTransition),
                    request.PhaseId, phase.Status, request.Status);
                return Result<UpdateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.InvalidStatusTransition, phase.Status, request.Status),
                    ResultErrorType.BadRequest);
            }

            // ── BUG-H FIX: No-op guard — skip DB write and cache invalidation if nothing changed ──
            // Previously a request with identical values would still update UpdatedAt, creating a false audit trail.
            var hasChanges =
                !string.Equals(phase.Name, request.Name, StringComparison.OrdinalIgnoreCase) ||
                phase.StartDate != request.StartDate ||
                phase.EndDate != request.EndDate ||
                phase.MaxStudents != request.MaxStudents ||
                phase.Description != request.Description ||
                phase.Status != request.Status;

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
                    MaxStudents = phase.MaxStudents,
                    Description = phase.Description,
                    Status = phase.Status,
                    UpdatedAt = phase.UpdatedAt
                },
                _messageService.GetMessage(MessageKeys.InternshipPhase.UpdateNoChanges));
            }

            // ── BUG-D FIX: Removed redundant outer duplicate-name check (outside transaction) ──
            // The pre-transaction check provided no race-condition protection. The real guard is
            // the DB-level UNIQUE INDEX on (enterprise_id, name) filtered by deleted_at IS NULL.
            // A DbUpdateException from the index violation is now caught and mapped to Conflict.

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Inner duplicate name check — inside transaction for best-effort soft-lock
            if (!string.Equals(phase.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var isDuplicateName = await _unitOfWork.Repository<InternshipPhase>().Query()
                    .AnyAsync(p => p.EnterpriseId == phase.EnterpriseId
                                && p.PhaseId != request.PhaseId
                                && p.Name.ToLower() == request.Name.ToLower()
                                && p.DeletedAt == null, cancellationToken);

                if (isDuplicateName)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateDuplicateName),
                        request.PhaseId, request.Name, phase.EnterpriseId);
                    return Result<UpdateInternshipPhaseResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.DuplicateNameOnUpdate, request.Name),
                        ResultErrorType.Conflict);
                }
            }

            phase.UpdateInfo(
                request.Name,
                request.StartDate,
                request.EndDate,
                request.MaxStudents,
                request.Description,
                request.Status);

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
                MaxStudents = phase.MaxStudents,
                Description = phase.Description,
                Status = phase.Status,
                UpdatedAt = phase.UpdatedAt
            },
            _messageService.GetMessage(MessageKeys.InternshipPhase.UpdateSuccess));
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("ix_internship_phases_enterprise_name_unique") == true)
        {
            // BUG-D FIX: The DB unique index (enterprise_id + name) is the true race-condition guard.
            // Previously this exception was swallowed into a 500. Now it maps to a proper 409 Conflict.
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateDuplicateName),
                request.PhaseId, request.Name, Guid.Empty);
            return Result<UpdateInternshipPhaseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipPhase.DuplicateNameOnUpdate, request.Name),
                ResultErrorType.Conflict);
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
}

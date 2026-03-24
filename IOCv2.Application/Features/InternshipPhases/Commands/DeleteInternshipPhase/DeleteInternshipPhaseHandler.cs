using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipPhases.Commands.DeleteInternshipPhase;

public class DeleteInternshipPhaseHandler
    : IRequestHandler<DeleteInternshipPhaseCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeleteInternshipPhaseHandler> _logger;
    private readonly ICacheService _cacheService;

    public DeleteInternshipPhaseHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<DeleteInternshipPhaseHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<bool>> Handle(
        DeleteInternshipPhaseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleting),
            request.PhaseId);

        try
        {
            var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
                .Include(p => p.InternshipGroups)
                .FirstOrDefaultAsync(p => p.PhaseId == request.PhaseId && p.DeletedAt == null, cancellationToken);

            if (phase == null)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleteNotFound),
                    request.PhaseId);
                return Result<bool>.NotFound(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound));
            }

            // ── Ownership check ──
            var role = _currentUserService.Role;
            if (role != "SuperAdmin" && role != "SchoolAdmin")
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                {
                    return Result<bool>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                        ResultErrorType.Unauthorized);
                }

                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null)
                {
                    return Result<bool>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseUserNotFound),
                        ResultErrorType.Forbidden);
                }

                if (enterpriseUser.EnterpriseId != phase.EnterpriseId)
                {
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                        currentUserId, phase.EnterpriseId, enterpriseUser.EnterpriseId);
                    return Result<bool>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                        ResultErrorType.Forbidden);
                }
            }

            // BUG-G FIX: Block deletion if ANY non-deleted group exists (Active OR Archived).
            // Previously only Active groups were checked; Archived (completed) groups were ignored,
            // making the phase deletable while leaving orphaned historical data that hides student history.
            var existingGroupCount = phase.InternshipGroups
                .Count(g => g.DeletedAt == null);

            if (existingGroupCount > 0)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleteHasActiveGroups),
                    phase.Name, request.PhaseId, existingGroupCount);
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.CannotDeleteHasActiveGroups, phase.Name, existingGroupCount),
                    ResultErrorType.BadRequest);
            }

            // Block deletion of InProgress phases
            if (phase.Status == InternshipPhaseStatus.InProgress)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleteInProgress),
                    phase.Name, request.PhaseId);
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.CannotDeleteInProgress, phase.Name),
                    ResultErrorType.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _unitOfWork.Repository<InternshipPhase>().DeleteAsync(phase);
            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

            if (saved > 0)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // BUG-I FIX: Remove over-broad PhasePattern() invalidation that previously evicted
                // ALL individual phase caches on every delete, causing a thundering-herd effect.
                // RemoveAsync(Phase(id)) already covers the deleted phase; PhaseListPattern covers lists.
                await _cacheService.RemoveAsync(InternshipPhaseCacheKeys.Phase(request.PhaseId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(InternshipPhaseCacheKeys.PhaseListPattern(), cancellationToken);

                _logger.LogInformation(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleteSuccess),
                    phase.PhaseId, phase.Name, phase.EnterpriseId);
                return Result<bool>.Success(true,
                    _messageService.GetMessage(MessageKeys.InternshipPhase.DeleteSuccess));
            }

            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleteNoChanges),
                request.PhaseId);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex,
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogDeleteError),
                request.PhaseId);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}

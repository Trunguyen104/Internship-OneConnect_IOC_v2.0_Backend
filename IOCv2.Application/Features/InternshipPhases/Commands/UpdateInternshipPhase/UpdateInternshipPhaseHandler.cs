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
            request.PhaseId, request.Name, "Lifecycle");

        var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
            .Include(p => p.InternshipGroups)
                .ThenInclude(g => g.Members)
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
        var lifecycleStatus = phase.GetLifecycleStatus(today);

        if (lifecycleStatus == InternshipPhaseLifecycleStatus.Ended)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateClosed),
                phase.Name, request.PhaseId);
            return Result<UpdateInternshipPhaseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipPhase.CannotUpdateEnded, phase.Name),
                ResultErrorType.BadRequest);
        }

        var hasGroups = phase.InternshipGroups.Any(g => g.DeletedAt == null);
        var placedCount = phase.InternshipGroups
            .Where(g => g.DeletedAt == null)
            .SelectMany(g => g.Members)
            .Select(m => m.StudentId)
            .Distinct()
            .Count();

        var lockFields = hasGroups || placedCount > 0;
        var isLockedFieldChange =
            request.StartDate != phase.StartDate ||
            request.EndDate != phase.EndDate ||
            request.Capacity != phase.Capacity;

        if (lockFields && isLockedFieldChange)
        {
            return Result<UpdateInternshipPhaseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipPhase.CannotUpdateLockedFields),
                ResultErrorType.BadRequest);
        }

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
                Status = lifecycleStatus,
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
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateDuplicateName),
                        request.PhaseId, request.Name, phase.EnterpriseId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<UpdateInternshipPhaseResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.DuplicateNameOnUpdate),
                        ResultErrorType.BadRequest);
                }
            }

            phase.UpdateInfo(
                request.Name,
                request.StartDate,
                request.EndDate,
                request.MajorFields,
                request.Capacity,
                request.Description,
                phase.Status);

            await _unitOfWork.Repository<InternshipPhase>().UpdateAsync(phase);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveAsync(InternshipPhaseCacheKeys.Phase(phase.PhaseId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(InternshipPhaseCacheKeys.PhaseListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(InternshipPhaseCacheKeys.PhaseEnterprisePattern(), cancellationToken);

            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogUpdateSuccess),
                phase.PhaseId, phase.Name);

            var updatedStatus = phase.GetLifecycleStatus(today);
            var remainingCapacity = Math.Max(phase.Capacity - placedCount, 0);

            return Result<UpdateInternshipPhaseResponse>.Success(new UpdateInternshipPhaseResponse
            {
                PhaseId = phase.PhaseId,
                EnterpriseId = phase.EnterpriseId,
                Name = phase.Name,
                StartDate = phase.StartDate,
                EndDate = phase.EndDate,
                MajorFields = phase.MajorFields,
                Capacity = phase.Capacity,
                RemainingCapacity = remainingCapacity,
                Description = phase.Description,
                Status = updatedStatus,
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
}

using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipPhases.Commands.CreateInternshipPhase;

public class CreateInternshipPhaseHandler
    : IRequestHandler<CreateInternshipPhaseCommand, Result<CreateInternshipPhaseResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<CreateInternshipPhaseHandler> _logger;
    private readonly ICacheService _cacheService;

    public CreateInternshipPhaseHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<CreateInternshipPhaseHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<CreateInternshipPhaseResponse>> Handle(
        CreateInternshipPhaseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogCreating),
            request.Name, request.EnterpriseId);

        // ── BUG-A FIX: Ownership check FIRST to prevent info leak via error-type difference ──
        // Non-admins who pass a wrong EnterpriseId would previously get NotFound (leaking whether
        // the enterprise exists). Now they always get Forbidden before we ever query for the enterprise.
        var role = _currentUserService.Role;
        if (role != "SuperAdmin" && role != "SchoolAdmin")
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<CreateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
            {
                return Result<CreateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            if (enterpriseUser.EnterpriseId != request.EnterpriseId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, request.EnterpriseId, enterpriseUser.EnterpriseId);
                return Result<CreateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                    ResultErrorType.Forbidden);
            }
        }

        // Enterprise existence check — only reached after ownership is confirmed
        var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
            .ExistsAsync(e => e.EnterpriseId == request.EnterpriseId, cancellationToken);

        if (!enterpriseExists)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogEnterpriseNotFound),
                request.EnterpriseId);
            return Result<CreateInternshipPhaseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Enterprise.NotFound),
                ResultErrorType.NotFound);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var hasDuplicateName = await _unitOfWork.Repository<InternshipPhase>().Query()
                .AnyAsync(p => p.EnterpriseId == request.EnterpriseId
                            && p.Name.ToLower() == request.Name.ToLower()
                            && p.DeletedAt == null, cancellationToken);

            if (hasDuplicateName)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogDuplicateName),
                    request.Name, request.EnterpriseId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<CreateInternshipPhaseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.DuplicateName),
                    ResultErrorType.BadRequest);
            }

            var phase = InternshipPhase.Create(
                request.EnterpriseId,
                request.Name,
                request.StartDate,
                request.EndDate,
                request.MajorFields,
                request.Capacity,
                request.Description);

            await _unitOfWork.Repository<InternshipPhase>().AddAsync(phase, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(InternshipPhaseCacheKeys.PhaseListPattern(), cancellationToken);

            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogCreateSuccess),
                phase.PhaseId, phase.Name, phase.EnterpriseId);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var lifecycleStatus = phase.GetLifecycleStatus(today);

            return Result<CreateInternshipPhaseResponse>.Success(new CreateInternshipPhaseResponse
            {
                PhaseId = phase.PhaseId,
                EnterpriseId = phase.EnterpriseId,
                Name = phase.Name,
                StartDate = phase.StartDate,
                EndDate = phase.EndDate,
                MajorFields = phase.MajorFields,
                Capacity = phase.Capacity,
                RemainingCapacity = phase.Capacity,
                Description = phase.Description,
                Status = lifecycleStatus,
                CreatedAt = phase.CreatedAt
            },
            _messageService.GetMessage(MessageKeys.InternshipPhase.CreateSuccess));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex,
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogCreateError),
                request.Name, request.EnterpriseId);
            return Result<CreateInternshipPhaseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}

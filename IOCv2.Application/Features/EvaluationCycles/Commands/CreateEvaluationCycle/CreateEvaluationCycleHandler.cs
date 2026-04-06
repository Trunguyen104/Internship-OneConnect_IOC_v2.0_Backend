using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;

public class CreateEvaluationCycleHandler
    : IRequestHandler<CreateEvaluationCycleCommand, Result<CreateEvaluationCycleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<CreateEvaluationCycleHandler> _logger;
    private readonly ICacheService _cacheService;

    public CreateEvaluationCycleHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<CreateEvaluationCycleHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<CreateEvaluationCycleResponse>> Handle(
        CreateEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating EvaluationCycle {Name} for Phase {PhaseId}", request.Name, request.PhaseId);

        var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
            .FirstOrDefaultAsync(p => p.PhaseId == request.PhaseId, cancellationToken);

        if (phase == null)
        {
            _logger.LogWarning("InternshipPhase {PhaseId} not found", request.PhaseId);
            return Result<CreateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.TermNotFound),
                ResultErrorType.NotFound);
        }

        var isDuplicateName = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AnyAsync(c => c.PhaseId == request.PhaseId && c.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (isDuplicateName)
        {
            _logger.LogWarning("EvaluationCycle {Name} already exists in Phase {PhaseId}", request.Name, request.PhaseId);
            return Result<CreateEvaluationCycleResponse>.Failure(
                "Tên đợt đánh giá đã tồn tại trong đợt thực tập này.",
                ResultErrorType.Conflict);
        }

        var requestStartDate = DateOnly.FromDateTime(request.StartDate);
        var requestEndDate = DateOnly.FromDateTime(request.EndDate);

        if (requestStartDate < phase.StartDate || requestEndDate > phase.EndDate)
        {
            _logger.LogWarning("EvaluationCycle dates {StartDate} to {EndDate} are out of bounds for Phase {PhaseId} ({PhaseStart} to {PhaseEnd})",
                request.StartDate, request.EndDate, request.PhaseId, phase.StartDate, phase.EndDate);
            return Result<CreateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.DatesOutOfBounds, phase.StartDate, phase.EndDate),
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var cycle = new EvaluationCycle
        {
            CycleId = Guid.NewGuid(),
            PhaseId = request.PhaseId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = EvaluationCycleStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<EvaluationCycle>().AddAsync(cycle, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CycleListPattern(), cancellationToken);

        _logger.LogInformation("Successfully created EvaluationCycle {CycleId}", cycle.CycleId);

        return Result<CreateEvaluationCycleResponse>.Success(new CreateEvaluationCycleResponse
        {
            CycleId = cycle.CycleId,
            PhaseId = cycle.PhaseId,
            Name = cycle.Name,
            StartDate = cycle.StartDate,
            EndDate = cycle.EndDate,
            Status = cycle.Status,

            CreatedAt = cycle.CreatedAt
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while creating EvaluationCycle for Phase {PhaseId}", request.PhaseId);
            return Result<CreateEvaluationCycleResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

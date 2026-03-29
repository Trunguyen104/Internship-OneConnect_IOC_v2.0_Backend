using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CompleteEvaluationCycle;

public class CompleteEvaluationCycleHandler
    : IRequestHandler<CompleteEvaluationCycleCommand, Result<CompleteEvaluationCycleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<CompleteEvaluationCycleHandler> _logger;
    private readonly ICacheService _cacheService;

    public CompleteEvaluationCycleHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<CompleteEvaluationCycleHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<CompleteEvaluationCycleResponse>> Handle(
        CompleteEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing EvaluationCycle {CycleId}", request.CycleId);

        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} not found", request.CycleId);
            return Result<CompleteEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);
        }

        if (cycle.Status == EvaluationCycleStatus.Completed || cycle.Status == EvaluationCycleStatus.Cancelled)
            return Result<CompleteEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.AlreadyCompleted),
                ResultErrorType.BadRequest);

        var hasCriteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .AnyAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (!hasCriteria)
            return Result<CompleteEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.CannotCompleteWithoutCriteria),
                ResultErrorType.BadRequest);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            cycle.Status = EvaluationCycleStatus.Completed;
            cycle.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<EvaluationCycle>().UpdateAsync(cycle, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CycleListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CyclePattern(), cancellationToken);

            _logger.LogInformation("Successfully completed EvaluationCycle {CycleId}", request.CycleId);

            return Result<CompleteEvaluationCycleResponse>.Success(new CompleteEvaluationCycleResponse
            {
                CycleId = cycle.CycleId,
                Status = cycle.Status.ToString(),
                UpdatedAt = cycle.UpdatedAt ?? DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while completing EvaluationCycle {CycleId}", request.CycleId);
            return Result<CompleteEvaluationCycleResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

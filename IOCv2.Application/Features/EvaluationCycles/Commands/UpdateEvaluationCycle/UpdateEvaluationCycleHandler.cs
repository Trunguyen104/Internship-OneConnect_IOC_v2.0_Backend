using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public class UpdateEvaluationCycleHandler
    : IRequestHandler<UpdateEvaluationCycleCommand, Result<UpdateEvaluationCycleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateEvaluationCycleHandler> _logger;
    private readonly ICacheService _cacheService;

    public UpdateEvaluationCycleHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<UpdateEvaluationCycleHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<UpdateEvaluationCycleResponse>> Handle(
        UpdateEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating EvaluationCycle {CycleId}", request.CycleId);

        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} not found", request.CycleId);
            return Result<UpdateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);
        }

        if (cycle.Status == EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot update EvaluationCycle {CycleId} because it is already completed", request.CycleId);
            return Result<UpdateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.CannotUpdateCompleted),
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);



        cycle.Name = request.Name;
        cycle.StartDate = request.StartDate;
        cycle.EndDate = request.EndDate;
        cycle.Status = request.Status;

        cycle.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<EvaluationCycle>().UpdateAsync(cycle, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CycleListPattern(), cancellationToken);
        await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CyclePattern(), cancellationToken);

        _logger.LogInformation("Successfully updated EvaluationCycle {CycleId}", request.CycleId);

        return Result<UpdateEvaluationCycleResponse>.Success(new UpdateEvaluationCycleResponse
        {
            CycleId = cycle.CycleId,
            PhaseId = cycle.PhaseId,
            Name = cycle.Name,
            StartDate = cycle.StartDate,
            EndDate = cycle.EndDate,
            Status = cycle.Status,

            UpdatedAt = cycle.UpdatedAt
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while updating EvaluationCycle {CycleId}", request.CycleId);
            return Result<UpdateEvaluationCycleResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

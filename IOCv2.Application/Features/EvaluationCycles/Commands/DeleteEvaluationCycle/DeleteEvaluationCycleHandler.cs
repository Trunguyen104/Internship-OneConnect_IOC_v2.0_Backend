using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.DeleteEvaluationCycle;

public class DeleteEvaluationCycleHandler
    : IRequestHandler<DeleteEvaluationCycleCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeleteEvaluationCycleHandler> _logger;
    private readonly ICacheService _cacheService;

    public DeleteEvaluationCycleHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<DeleteEvaluationCycleHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<bool>> Handle(
        DeleteEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting EvaluationCycle {CycleId}", request.CycleId);

        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} not found", request.CycleId);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);
        }

        if (cycle.Status == Domain.Enums.EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot delete EvaluationCycle {CycleId} because it is already completed", request.CycleId);
            // Có thể định nghĩa key riêng, tạm dùng CannotUpdateCompleted / Validation Fail
            return Result<bool>.Failure(
                "Không thể xóa chu kỳ đánh giá đã hoàn thành.",
                ResultErrorType.BadRequest);
        }

        var hasCriteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .AnyAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (hasCriteria)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} cannot be deleted because it has associated criteria", request.CycleId);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.CannotDeleteWithCriteria),
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Soft delete via UpdatedAt — EF global filter handles DeletedAt
        cycle.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<EvaluationCycle>().UpdateAsync(cycle, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CycleListPattern(), cancellationToken);
        await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CyclePattern(), cancellationToken);

        _logger.LogInformation("Successfully deleted EvaluationCycle {CycleId}", request.CycleId);

        return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while deleting EvaluationCycle {CycleId}", request.CycleId);
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

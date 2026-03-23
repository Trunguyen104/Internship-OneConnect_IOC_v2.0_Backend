using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.StartEvaluationCycle;

public class StartEvaluationCycleHandler
    : IRequestHandler<StartEvaluationCycleCommand, Result<StartEvaluationCycleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<StartEvaluationCycleHandler> _logger;
    private readonly ICacheService _cacheService;

    public StartEvaluationCycleHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<StartEvaluationCycleHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<StartEvaluationCycleResponse>> Handle(
        StartEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting EvaluationCycle {CycleId}", request.CycleId);

        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} not found", request.CycleId);
            return Result<StartEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);
        }

        if (cycle.Status != EvaluationCycleStatus.Pending)
        {
            _logger.LogWarning("Cannot start EvaluationCycle {CycleId} because it is not in Pending status (Current: {Status})", request.CycleId, cycle.Status);
            return Result<StartEvaluationCycleResponse>.Failure(
                "Chỉ có thể bắt đầu (Start) đợt đánh giá khi nó đang ở trạng thái Khởi tạo (Pending).",
                ResultErrorType.BadRequest);
        }

        var hasCriteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .AnyAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (!hasCriteria)
        {
            _logger.LogWarning("Cannot start EvaluationCycle {CycleId} because it has no criteria configured.", request.CycleId);
            return Result<StartEvaluationCycleResponse>.Failure(
                "Không thể bắt đầu đợt đánh giá vì chưa có bất kỳ Tiêu chí chấm điểm (Criteria) nào được cấu hình.",
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            cycle.Status = EvaluationCycleStatus.Grading;
            cycle.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<EvaluationCycle>().UpdateAsync(cycle, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CycleListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(EvaluationCycleCacheKeys.CyclePattern(), cancellationToken);

            _logger.LogInformation("Successfully started EvaluationCycle {CycleId}", request.CycleId);

            return Result<StartEvaluationCycleResponse>.Success(new StartEvaluationCycleResponse
            {
                CycleId = cycle.CycleId,
                Status = cycle.Status.ToString(),
                UpdatedAt = cycle.UpdatedAt ?? DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while starting EvaluationCycle {CycleId}", request.CycleId);
            return Result<StartEvaluationCycleResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

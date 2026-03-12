using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.DeleteEvaluationCriteria;

public class DeleteEvaluationCriteriaHandler
    : IRequestHandler<DeleteEvaluationCriteriaCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeleteEvaluationCriteriaHandler> _logger;

    public DeleteEvaluationCriteriaHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<DeleteEvaluationCriteriaHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        DeleteEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting EvaluationCriteria {CriteriaId}", request.CriteriaId);

        var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .FirstOrDefaultAsync(c => c.CriteriaId == request.CriteriaId, cancellationToken);

        if (criteria is null)
        {
            _logger.LogWarning("EvaluationCriteria {CriteriaId} not found", request.CriteriaId);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.NotFound),
                ResultErrorType.NotFound);
        }

        var cycle = await _unitOfWork.Repository<Domain.Entities.EvaluationCycle>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CycleId == criteria.CycleId, cancellationToken);

        if (cycle!.Status == Domain.Enums.EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot delete criteria: EvaluationCycle {CycleId} is already completed", criteria.CycleId);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.CannotDeleteInCompletedCycle),
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        criteria.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().UpdateAsync(criteria, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted EvaluationCriteria {CriteriaId}", request.CriteriaId);

        return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while deleting EvaluationCriteria {CriteriaId}", request.CriteriaId);
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

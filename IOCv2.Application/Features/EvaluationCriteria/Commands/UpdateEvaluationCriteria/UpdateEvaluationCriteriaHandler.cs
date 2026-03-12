using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.UpdateEvaluationCriteria;

public class UpdateEvaluationCriteriaHandler
    : IRequestHandler<UpdateEvaluationCriteriaCommand, Result<UpdateEvaluationCriteriaResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateEvaluationCriteriaHandler> _logger;

    public UpdateEvaluationCriteriaHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<UpdateEvaluationCriteriaHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<UpdateEvaluationCriteriaResponse>> Handle(
        UpdateEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating EvaluationCriteria {CriteriaId}", request.CriteriaId);

        var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .FirstOrDefaultAsync(c => c.CriteriaId == request.CriteriaId, cancellationToken);

        if (criteria is null)
        {
            _logger.LogWarning("EvaluationCriteria {CriteriaId} not found", request.CriteriaId);
            return Result<UpdateEvaluationCriteriaResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.NotFound),
                ResultErrorType.NotFound);
        }

        var cycle = await _unitOfWork.Repository<Domain.Entities.EvaluationCycle>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CycleId == criteria.CycleId, cancellationToken);

        if (cycle!.Status == Domain.Enums.EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot update criteria: EvaluationCycle {CycleId} is already completed", criteria.CycleId);
            return Result<UpdateEvaluationCriteriaResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.CannotUpdateInCompletedCycle),
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        criteria.Name = request.Name;
        criteria.Description = request.Description;
        criteria.MaxScore = request.MaxScore;
        criteria.Weight = request.Weight;
        criteria.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().UpdateAsync(criteria, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully updated EvaluationCriteria {CriteriaId}", request.CriteriaId);

        return Result<UpdateEvaluationCriteriaResponse>.Success(new UpdateEvaluationCriteriaResponse
        {
            CriteriaId = criteria.CriteriaId,
            CycleId = criteria.CycleId,
            Name = criteria.Name,
            Description = criteria.Description,
            MaxScore = criteria.MaxScore,
            Weight = criteria.Weight,
            UpdatedAt = criteria.UpdatedAt
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while updating EvaluationCriteria {CriteriaId}", request.CriteriaId);
            return Result<UpdateEvaluationCriteriaResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

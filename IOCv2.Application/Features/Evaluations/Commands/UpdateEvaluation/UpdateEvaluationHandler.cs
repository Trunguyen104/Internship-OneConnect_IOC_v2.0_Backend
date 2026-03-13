using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Evaluations.Commands.UpdateEvaluation;

public class UpdateEvaluationHandler : IRequestHandler<UpdateEvaluationCommand, Result<UpdateEvaluationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateEvaluationHandler> _logger;

    public UpdateEvaluationHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<UpdateEvaluationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<UpdateEvaluationResponse>> Handle(
        UpdateEvaluationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating Evaluation {EvaluationId}", request.EvaluationId);

        // 1. Lấy Evaluation
        var evaluation = await _unitOfWork.Repository<Evaluation>().Query()
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, cancellationToken);

        if (evaluation is null)
        {
            _logger.LogWarning("Evaluation {EvaluationId} not found", request.EvaluationId);
            return Result<UpdateEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);
        }

        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CycleId == evaluation.CycleId, cancellationToken);

        if (cycle!.Status == EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot update evaluation: EvaluationCycle {CycleId} is already completed", evaluation.CycleId);
            return Result<UpdateEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CannotUpdateInCompletedCycle),
                ResultErrorType.BadRequest);
        }

        // 2. Chỉ cho update khi Status = Draft
        if (evaluation.Status != EvaluationStatus.Draft)
        {
            _logger.LogWarning("Evaluation {EvaluationId} cannot be updated because its status is {Status}", request.EvaluationId, evaluation.Status);
            return Result<UpdateEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CannotUpdateSubmitted),
                ResultErrorType.BadRequest);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            decimal? totalScore = null;

            if (request.Details.Count > 0)
            {
                var criteriaIds = request.Details.Select(d => d.CriteriaId).ToList();
                var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
                    .Where(c => criteriaIds.Contains(c.CriteriaId) && c.CycleId == evaluation.CycleId)
                    .ToListAsync(cancellationToken);

                if (criteria.Count != criteriaIds.Count)
                    return Result<UpdateEvaluationResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.EvaluationKey.CriteriaNotFound),
                        ResultErrorType.BadRequest);

                // Check score không vượt MaxScore
                foreach (var detail in request.Details)
                {
                    var criterion = criteria.First(c => c.CriteriaId == detail.CriteriaId);
                    if (detail.Score > criterion.MaxScore)
                        return Result<UpdateEvaluationResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.EvaluationKey.ScoreExceedsMax),
                            ResultErrorType.BadRequest);
                }

                // Xóa Details cũ, thêm Details mới (replace strategy)
                foreach (var oldDetail in evaluation.Details.ToList())
                    await _unitOfWork.Repository<EvaluationDetail>().DeleteAsync(oldDetail, cancellationToken);

                foreach (var detail in request.Details)
                {
                    var evalDetail = new EvaluationDetail
                    {
                        DetailId = Guid.NewGuid(),
                        EvaluationId = evaluation.EvaluationId,
                        CriteriaId = detail.CriteriaId,
                        Score = detail.Score,
                        Comment = detail.Comment,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<EvaluationDetail>().AddAsync(evalDetail, cancellationToken);
                }

                // Tính lại weighted total score
                totalScore = criteria.Sum(c =>
                {
                    var detail = request.Details.First(d => d.CriteriaId == c.CriteriaId);
                    return (detail.Score / c.MaxScore) * c.Weight;
                });
            }

            // Update evaluation
            evaluation.Note = request.Note ?? evaluation.Note;
            evaluation.TotalScore = totalScore ?? evaluation.TotalScore;
            evaluation.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Evaluation>().UpdateAsync(evaluation, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully updated Evaluation {EvaluationId}", evaluation.EvaluationId);

            return Result<UpdateEvaluationResponse>.Success(new UpdateEvaluationResponse
            {
                EvaluationId = evaluation.EvaluationId,
                Status = evaluation.Status,

                TotalScore = evaluation.TotalScore,
                Note = evaluation.Note,
                DetailCount = request.Details.Count > 0 ? request.Details.Count : evaluation.Details.Count,
                UpdatedAt = evaluation.UpdatedAt ?? DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while updating Evaluation {EvaluationId}", request.EvaluationId);
            return Result<UpdateEvaluationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

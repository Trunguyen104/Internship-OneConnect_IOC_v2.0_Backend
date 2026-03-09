using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Evaluations.Commands.SubmitEvaluation;

public class SubmitEvaluationHandler : IRequestHandler<SubmitEvaluationCommand, Result<SubmitEvaluationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<SubmitEvaluationHandler> _logger;

    public SubmitEvaluationHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<SubmitEvaluationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<SubmitEvaluationResponse>> Handle(
        SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting Evaluation {EvaluationId}", request.EvaluationId);

        var evaluation = await _unitOfWork.Repository<Evaluation>().Query()
            .FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, cancellationToken);

        if (evaluation is null)
        {
            _logger.LogWarning("Evaluation {EvaluationId} not found", request.EvaluationId);
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);
        }

        if (evaluation.Status != EvaluationStatus.Draft)
        {
            _logger.LogWarning("Evaluation {EvaluationId} cannot be submitted because its status is {Status}", request.EvaluationId, evaluation.Status);
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.AlreadySubmitted),
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        evaluation.Status = EvaluationStatus.Submitted;
        evaluation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Evaluation>().UpdateAsync(evaluation, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully submitted Evaluation {EvaluationId}", request.EvaluationId);

        return Result<SubmitEvaluationResponse>.Success(new SubmitEvaluationResponse
        {
            EvaluationId = evaluation.EvaluationId,
            Status = evaluation.Status,

            TotalScore = evaluation.TotalScore,
            UpdatedAt = evaluation.UpdatedAt ?? DateTime.UtcNow
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while submitting Evaluation {EvaluationId}", request.EvaluationId);
            return Result<SubmitEvaluationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

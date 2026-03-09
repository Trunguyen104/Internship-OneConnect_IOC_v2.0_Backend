using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;

public class PublishEvaluationHandler : IRequestHandler<PublishEvaluationCommand, Result<PublishEvaluationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<PublishEvaluationHandler> _logger;

    public PublishEvaluationHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<PublishEvaluationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<PublishEvaluationResponse>> Handle(
        PublishEvaluationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing Evaluation {EvaluationId}", request.EvaluationId);

        var evaluation = await _unitOfWork.Repository<Evaluation>().Query()
            .FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, cancellationToken);

        if (evaluation is null)
        {
            _logger.LogWarning("Evaluation {EvaluationId} not found", request.EvaluationId);
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);
        }

        // Chỉ Publish được khi đã Submitted
        if (evaluation.Status != EvaluationStatus.Submitted)
        {
            _logger.LogWarning("Evaluation {EvaluationId} must be in Submitted status to publish, but it is {Status}", request.EvaluationId, evaluation.Status);
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CannotPublishIfNotSubmitted), // assuming this exists, or use generic
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        evaluation.Status = EvaluationStatus.Published;
        evaluation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Evaluation>().UpdateAsync(evaluation, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully published Evaluation {EvaluationId}", request.EvaluationId);

        return Result<PublishEvaluationResponse>.Success(new PublishEvaluationResponse
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
            _logger.LogError(ex, "Error occurred while publishing Evaluation {EvaluationId}", request.EvaluationId);
            return Result<PublishEvaluationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

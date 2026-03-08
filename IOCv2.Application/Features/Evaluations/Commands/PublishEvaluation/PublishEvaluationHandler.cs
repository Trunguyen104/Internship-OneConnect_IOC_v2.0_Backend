using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;

public class PublishEvaluationHandler : IRequestHandler<PublishEvaluationCommand, Result<PublishEvaluationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public PublishEvaluationHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<PublishEvaluationResponse>> Handle(
        PublishEvaluationCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _unitOfWork.Repository<Evaluation>().Query()
            .FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);

        // Chỉ Publish được khi đã Submitted
        if (evaluation.Status != EvaluationStatus.Submitted)
            return Result<PublishEvaluationResponse>.Failure(
                "Evaluation must be in Submitted status to publish.",
                ResultErrorType.BadRequest);

        evaluation.Status = EvaluationStatus.Published;
        evaluation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Evaluation>().UpdateAsync(evaluation, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<PublishEvaluationResponse>.Success(new PublishEvaluationResponse
        {
            EvaluationId = evaluation.EvaluationId,
            Status = evaluation.Status,

            TotalScore = evaluation.TotalScore,
            UpdatedAt = evaluation.UpdatedAt ?? DateTime.UtcNow
        });
    }
}

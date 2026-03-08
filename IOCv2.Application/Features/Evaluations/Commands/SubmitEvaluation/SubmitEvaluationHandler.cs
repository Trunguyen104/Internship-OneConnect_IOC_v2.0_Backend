using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Evaluations.Commands.SubmitEvaluation;

public class SubmitEvaluationHandler : IRequestHandler<SubmitEvaluationCommand, Result<SubmitEvaluationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public SubmitEvaluationHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<SubmitEvaluationResponse>> Handle(
        SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _unitOfWork.Repository<Evaluation>().Query()
            .FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);

        if (evaluation.Status != EvaluationStatus.Draft)
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.AlreadySubmitted),
                ResultErrorType.BadRequest);

        evaluation.Status = EvaluationStatus.Submitted;
        evaluation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Evaluation>().UpdateAsync(evaluation, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<SubmitEvaluationResponse>.Success(new SubmitEvaluationResponse
        {
            EvaluationId = evaluation.EvaluationId,
            Status = evaluation.Status,

            TotalScore = evaluation.TotalScore,
            UpdatedAt = evaluation.UpdatedAt ?? DateTime.UtcNow
        });
    }
}

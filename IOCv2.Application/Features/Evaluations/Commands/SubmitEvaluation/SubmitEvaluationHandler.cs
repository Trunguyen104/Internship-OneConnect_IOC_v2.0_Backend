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
        var evaluations = await _unitOfWork.Repository<Evaluation>().Query()
            .Where(e => e.CycleId == request.CycleId && e.InternshipId == request.InternshipId)
            .ToListAsync(cancellationToken);

        if (request.StudentIds != null && request.StudentIds.Any())
        {
            evaluations = evaluations.Where(e => request.StudentIds.Contains(e.StudentId)).ToList();
        }

        if (evaluations.Count == 0)
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);

        var drafts = evaluations.Where(e => e.Status == EvaluationStatus.Draft || e.Status == EvaluationStatus.Pending).ToList();
        
        if (drafts.Count == 0)
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.AlreadySubmitted),
                ResultErrorType.BadRequest);

        var now = DateTime.UtcNow;
        foreach (var eval in drafts)
        {
            eval.Status = EvaluationStatus.Submitted;
            eval.UpdatedAt = now;
            await _unitOfWork.Repository<Evaluation>().UpdateAsync(eval, cancellationToken);
        }

        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<SubmitEvaluationResponse>.Success(new SubmitEvaluationResponse
        {
            CycleId = request.CycleId,
            InternshipId = request.InternshipId,
            UpdatedCount = drafts.Count,
            Status = EvaluationStatus.Submitted.ToString(),
            UpdatedAt = now
        });
    }
}

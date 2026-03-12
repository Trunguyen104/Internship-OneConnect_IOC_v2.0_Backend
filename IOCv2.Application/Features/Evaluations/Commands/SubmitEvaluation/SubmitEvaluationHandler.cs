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
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);
            
        if (cycle == null)
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CycleNotFound),
                ResultErrorType.NotFound);

        if (cycle.Status == EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot submit evaluation: EvaluationCycle {CycleId} is already completed", request.CycleId);
            return Result<SubmitEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CannotSubmitInCompletedCycle),
                ResultErrorType.BadRequest);
        }

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

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var eval in drafts)
        {
            eval.Status = EvaluationStatus.Submitted;
            eval.UpdatedAt = now;
            await _unitOfWork.Repository<Evaluation>().UpdateAsync(eval, cancellationToken);
        }

        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully submitted Evaluations for Cycle {CycleId} and Internship {InternshipId}", request.CycleId, request.InternshipId);

        return Result<SubmitEvaluationResponse>.Success(new SubmitEvaluationResponse
        {
            CycleId = request.CycleId,
            InternshipId = request.InternshipId,
            UpdatedCount = drafts.Count,
            Status = EvaluationStatus.Submitted.ToString(),
            UpdatedAt = now
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while submitting Evaluations for Cycle {CycleId} and Internship {InternshipId}", request.CycleId, request.InternshipId);
            return Result<SubmitEvaluationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

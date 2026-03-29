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
        _logger.LogInformation("Publishing Evaluations for Cycle {CycleId} and Internship {InternshipId}", request.CycleId, request.InternshipId);

        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);
            
        if (cycle == null)
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CycleNotFound),
                ResultErrorType.NotFound);

        if (cycle.Status == EvaluationCycleStatus.Completed)
        {
            _logger.LogWarning("Cannot publish evaluation: EvaluationCycle {CycleId} is already completed", request.CycleId);
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CannotPublishInCompletedCycle),
                ResultErrorType.BadRequest);
        }

        var internship = await _unitOfWork.Repository<InternshipGroup>().Query()
            .Include(i => i.Members)
            .FirstOrDefaultAsync(i => i.InternshipId == request.InternshipId, cancellationToken);

        if (internship == null)
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.InternshipNotFound),
                ResultErrorType.NotFound);

        var query = _unitOfWork.Repository<Evaluation>().Query()
            .Where(e => e.CycleId == request.CycleId && e.InternshipId == request.InternshipId);

        if (request.StudentIds != null && request.StudentIds.Count > 0)
        {
            query = query.Where(e => request.StudentIds.Contains(e.StudentId!.Value));
        }

        var evaluations = await query.ToListAsync(cancellationToken);

        if (evaluations.Count == 0)
            return Result<PublishEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.NotFound),
                ResultErrorType.NotFound);

        // AC-05: Phiếu Pending (chưa chấm) không thể Publish
        var pendingEvals = evaluations.Where(e => e.Status == EvaluationStatus.Pending).ToList();
        if (pendingEvals.Count > 0)
        {
            return Result<PublishEvaluationResponse>.Failure(
                "Không thể công bố phiếu điểm chưa được chấm (Pending).",
                ResultErrorType.BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var now = DateTime.UtcNow;
            foreach (var eval in evaluations)
            {
                eval.Status = EvaluationStatus.Published;
                eval.UpdatedAt = now;
                await _unitOfWork.Repository<Evaluation>().UpdateAsync(eval, cancellationToken);
            }

        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully published Evaluations for Cycle {CycleId} and Internship {InternshipId}", request.CycleId, request.InternshipId);

        return Result<PublishEvaluationResponse>.Success(new PublishEvaluationResponse
        {
            CycleId = request.CycleId,
            InternshipId = request.InternshipId,
            UpdatedCount = evaluations.Count,
            Status = EvaluationStatus.Published.ToString(),
            UpdatedAt = now
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while publishing Evaluations for Cycle {CycleId} and Internship {InternshipId}", request.CycleId, request.InternshipId);
            return Result<PublishEvaluationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

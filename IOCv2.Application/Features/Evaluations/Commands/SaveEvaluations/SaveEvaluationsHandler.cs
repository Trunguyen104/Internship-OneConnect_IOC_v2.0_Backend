using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Evaluations.Commands.SaveEvaluations;

public class SaveEvaluationsHandler : IRequestHandler<SaveEvaluationsCommand, Result<List<SaveEvaluationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public SaveEvaluationsHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<List<SaveEvaluationsResponse>>> Handle(
        SaveEvaluationsCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Cycle
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);
        if (cycle is null)
            return Result<List<SaveEvaluationsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CycleNotFound),
                ResultErrorType.NotFound);

        // 2. Validate InternshipGroup
        var internship = await _unitOfWork.Repository<InternshipGroup>().Query()
            .Include(i => i.Members)
            .FirstOrDefaultAsync(i => i.InternshipId == request.InternshipId, cancellationToken);
        if (internship is null)
            return Result<List<SaveEvaluationsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.InternshipNotFound),
                ResultErrorType.NotFound);

        // 3. Pre-load check data
        var existingEvaluations = await _unitOfWork.Repository<Evaluation>().Query()
            .Include(e => e.Details)
            .Where(e => e.CycleId == request.CycleId && e.InternshipId == request.InternshipId)
            .ToListAsync(cancellationToken);

        var allCriteriaIds = request.Evaluations.SelectMany(e => e.Details).Select(d => d.CriteriaId).Distinct().ToList();
        var criteriaList = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .Where(c => allCriteriaIds.Contains(c.CriteriaId) && c.CycleId == request.CycleId)
            .ToListAsync(cancellationToken);

        // Map for Student Names
        var studentNames = new Dictionary<Guid, string>();
        var studentIdsInRequest = request.Evaluations.Where(e => e.StudentId.HasValue).Select(e => e.StudentId!.Value).ToList();
        if (studentIdsInRequest.Count > 0)
        {
            var students = await _unitOfWork.Repository<Student>().Query()
                .Include(s => s.User)
                .Where(s => studentIdsInRequest.Contains(s.StudentId))
                .ToListAsync(cancellationToken);
            studentNames = students.ToDictionary(s => s.StudentId, s => s.User?.FullName ?? string.Empty);
        }

        var results = new List<SaveEvaluationsResponse>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            foreach (var evalInput in request.Evaluations)
            {
                var isGroupEvaluation = !evalInput.StudentId.HasValue;

                // Validate Student in group
                if (!isGroupEvaluation)
                {
                    var studentInGroup = internship.Members.Any(m => m.StudentId == evalInput.StudentId!.Value);
                    if (!studentInGroup)
                        return Result<List<SaveEvaluationsResponse>>.Failure(
                            $"Student {evalInput.StudentId} is not in the internship group.",
                            ResultErrorType.BadRequest);
                }

                // Validate Criteria & Score
                decimal? totalScore = null;
                if (evalInput.Details.Count > 0)
                {
                    foreach (var detail in evalInput.Details)
                    {
                        var criterion = criteriaList.FirstOrDefault(c => c.CriteriaId == detail.CriteriaId);
                        if (criterion == null)
                            return Result<List<SaveEvaluationsResponse>>.Failure(
                                _messageService.GetMessage(MessageKeys.EvaluationKey.CriteriaNotFound),
                                ResultErrorType.BadRequest);

                        if (detail.Score > criterion.MaxScore)
                            return Result<List<SaveEvaluationsResponse>>.Failure(
                                _messageService.GetMessage(MessageKeys.EvaluationKey.ScoreExceedsMax),
                                ResultErrorType.BadRequest);
                    }

                    totalScore = evalInput.Details.Sum(detail =>
                    {
                        var criterion = criteriaList.First(c => c.CriteriaId == detail.CriteriaId);
                        return (detail.Score / criterion.MaxScore) * criterion.Weight;
                    });
                }

                string studentName = string.Empty;
                if (!isGroupEvaluation && studentNames.TryGetValue(evalInput.StudentId!.Value, out var sName))
                {
                    studentName = sName;
                }

                var existingEval = existingEvaluations.FirstOrDefault(e => e.StudentId == evalInput.StudentId);
                
                if (existingEval != null)
                {
                    // Update existing
                    existingEval.TotalScore = totalScore;
                    existingEval.Note = evalInput.Note;
                    existingEval.UpdatedAt = now;
                    // Note: We don't change the Status. If it was Submitted/Published, it remains so.

                    await _unitOfWork.Repository<Evaluation>().UpdateAsync(existingEval, cancellationToken);
                    
                    // Upsert Details
                    foreach (var detailInput in evalInput.Details)
                    {
                        var existingDetail = existingEval.Details.FirstOrDefault(d => d.CriteriaId == detailInput.CriteriaId);
                        if (existingDetail != null)
                        {
                            existingDetail.Score = detailInput.Score;
                            existingDetail.Comment = detailInput.Comment;
                            existingDetail.UpdatedAt = now;
                            await _unitOfWork.Repository<EvaluationDetail>().UpdateAsync(existingDetail, cancellationToken);
                        }
                        else
                        {
                            var newDetail = new EvaluationDetail
                            {
                                DetailId = Guid.NewGuid(),
                                EvaluationId = existingEval.EvaluationId,
                                CriteriaId = detailInput.CriteriaId,
                                Score = detailInput.Score,
                                Comment = detailInput.Comment,
                                CreatedAt = now
                            };
                            await _unitOfWork.Repository<EvaluationDetail>().AddAsync(newDetail, cancellationToken);
                        }
                    }

                    results.Add(new SaveEvaluationsResponse
                    {
                        EvaluationId = existingEval.EvaluationId,
                        CycleId = existingEval.CycleId,
                        CycleName = cycle.Name,
                        InternshipId = existingEval.InternshipId,
                        IsGroupEvaluation = isGroupEvaluation,
                        StudentId = existingEval.StudentId,
                        StudentName = studentName,
                        EvaluatorId = existingEval.EvaluatorId,
                        Status = existingEval.Status,
                        TotalScore = existingEval.TotalScore,
                        Note = existingEval.Note,
                        DetailCount = existingEval.Details.Count,
                        CreatedAt = existingEval.CreatedAt
                    });
                }
                else
                {
                    // Create new
                    var newEval = new Evaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        CycleId = request.CycleId,
                        InternshipId = request.InternshipId,
                        StudentId = evalInput.StudentId,
                        EvaluatorId = request.EvaluatorId,
                        Status = EvaluationStatus.Draft,
                        TotalScore = totalScore,
                        Note = evalInput.Note,
                        CreatedAt = now
                    };

                    await _unitOfWork.Repository<Evaluation>().AddAsync(newEval, cancellationToken);

                    foreach (var detailInput in evalInput.Details)
                    {
                        var newDetail = new EvaluationDetail
                        {
                            DetailId = Guid.NewGuid(),
                            EvaluationId = newEval.EvaluationId,
                            CriteriaId = detailInput.CriteriaId,
                            Score = detailInput.Score,
                            Comment = detailInput.Comment,
                            CreatedAt = now
                        };
                        await _unitOfWork.Repository<EvaluationDetail>().AddAsync(newDetail, cancellationToken);
                    }

                    results.Add(new SaveEvaluationsResponse
                    {
                        EvaluationId = newEval.EvaluationId,
                        CycleId = newEval.CycleId,
                        CycleName = cycle.Name,
                        InternshipId = newEval.InternshipId,
                        IsGroupEvaluation = isGroupEvaluation,
                        StudentId = newEval.StudentId,
                        StudentName = studentName,
                        EvaluatorId = newEval.EvaluatorId,
                        Status = newEval.Status,
                        TotalScore = newEval.TotalScore,
                        Note = newEval.Note,
                        DetailCount = evalInput.Details.Count,
                        CreatedAt = newEval.CreatedAt
                    });
                }
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<List<SaveEvaluationsResponse>>.Success(results);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

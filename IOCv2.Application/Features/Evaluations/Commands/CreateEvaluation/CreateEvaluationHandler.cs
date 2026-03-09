using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Evaluations.Commands.CreateEvaluation;

public class CreateEvaluationHandler : IRequestHandler<CreateEvaluationCommand, Result<CreateEvaluationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<CreateEvaluationHandler> _logger;

    public CreateEvaluationHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<CreateEvaluationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<CreateEvaluationResponse>> Handle(
        CreateEvaluationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating Evaluation for Internship {InternshipId}, Cycle {CycleId}, Student {StudentId}", 
            request.InternshipId, request.CycleId, request.StudentId);

        var isGroupEvaluation = !request.StudentId.HasValue;

        // 1. Validate Cycle tồn tại
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
            return Result<CreateEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CycleNotFound),
                ResultErrorType.NotFound);

        // 2. Validate InternshipGroup tồn tại
        var internship = await _unitOfWork.Repository<InternshipGroup>().Query()
            .Include(i => i.Members)
            .FirstOrDefaultAsync(i => i.InternshipId == request.InternshipId, cancellationToken);

        if (internship is null)
            return Result<CreateEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.InternshipNotFound),
                ResultErrorType.NotFound);

        // 3. Validate Student (chỉ khi Individual evaluation)
        string studentName = string.Empty;
        if (!isGroupEvaluation)
        {
            var studentInGroup = internship.Members.Any(m => m.StudentId == request.StudentId!.Value);
            if (!studentInGroup)
                return Result<CreateEvaluationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.EvaluationKey.StudentNotInGroup),
                    ResultErrorType.BadRequest);
        }

        // 4. Validate chưa có evaluation trùng (CycleId + InternshipId + StudentId)
        var exists = await _unitOfWork.Repository<Evaluation>().Query()
            .AnyAsync(e => e.CycleId == request.CycleId
                        && e.InternshipId == request.InternshipId
                        && e.StudentId == request.StudentId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Evaluation already exists for Cycle {CycleId}, Internship {InternshipId}, Student {StudentId}", 
                request.CycleId, request.InternshipId, request.StudentId);
            return Result<CreateEvaluationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.AlreadyExists),
                ResultErrorType.Conflict);
        }

        // 5. Validate criteria và tính TotalScore nếu có Details
        decimal? totalScore = null;
        if (request.Details.Count > 0)
        {
            var criteriaIds = request.Details.Select(d => d.CriteriaId).ToList();
            var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
                .Where(c => criteriaIds.Contains(c.CriteriaId) && c.CycleId == request.CycleId)
                .ToListAsync(cancellationToken);

            if (criteria.Count != criteriaIds.Count)
                return Result<CreateEvaluationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.EvaluationKey.CriteriaNotFound),
                    ResultErrorType.BadRequest);

            foreach (var detail in request.Details)
            {
                var criterion = criteria.First(c => c.CriteriaId == detail.CriteriaId);
                if (detail.Score > criterion.MaxScore)
                    return Result<CreateEvaluationResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.EvaluationKey.ScoreExceedsMax),
                        ResultErrorType.BadRequest);
            }

            totalScore = criteria.Sum(c =>
            {
                var detail = request.Details.First(d => d.CriteriaId == c.CriteriaId);
                return (detail.Score / c.MaxScore) * c.Weight;
            });
        }

        // 6. Tạo Evaluation + Details trong transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var evaluation = new Evaluation
            {
                EvaluationId = Guid.NewGuid(),
                CycleId = request.CycleId,
                InternshipId = request.InternshipId,
                StudentId = request.StudentId,     // null → Group evaluation
                EvaluatorId = request.EvaluatorId,
                Status = EvaluationStatus.Draft,
                TotalScore = totalScore,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Evaluation>().AddAsync(evaluation, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

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

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Lấy StudentName nếu là individual
            if (!isGroupEvaluation)
            {
                var student = await _unitOfWork.Repository<Student>().Query()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentId == request.StudentId!.Value, cancellationToken);
                studentName = student?.User?.FullName ?? string.Empty;
            }

            _logger.LogInformation("Successfully created Evaluation {EvaluationId}", evaluation.EvaluationId);

            return Result<CreateEvaluationResponse>.Success(new CreateEvaluationResponse
            {
                EvaluationId = evaluation.EvaluationId,
                CycleId = evaluation.CycleId,
                CycleName = cycle.Name,
                InternshipId = evaluation.InternshipId,
                IsGroupEvaluation = isGroupEvaluation,
                StudentId = evaluation.StudentId,
                StudentName = studentName,
                EvaluatorId = evaluation.EvaluatorId,
                Status = evaluation.Status,

                TotalScore = evaluation.TotalScore,
                Note = evaluation.Note,
                DetailCount = request.Details.Count,
                CreatedAt = evaluation.CreatedAt
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while creating Evaluation for Internship {InternshipId}", request.InternshipId);
            return Result<CreateEvaluationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

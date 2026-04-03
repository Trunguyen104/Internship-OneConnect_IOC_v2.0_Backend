using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentEvaluations;

public class GetUniAdminStudentEvaluationsHandler
    : IRequestHandler<GetUniAdminStudentEvaluationsQuery, Result<GetUniAdminStudentEvaluationsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentEvaluationsHandler> _logger;

    public GetUniAdminStudentEvaluationsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentEvaluationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentEvaluationsResponse>> Handle(
        GetUniAdminStudentEvaluationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentEvaluationsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        // Get UniversityId
        var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

        if (universityUser == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound),
                currentUserId);
            return Result<GetUniAdminStudentEvaluationsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                ResultErrorType.Forbidden);
        }

        var universityId = universityUser.UniversityId;

        // Resolve Term
        Term? term;
        if (request.TermId.HasValue)
        {
            term = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId.Value, cancellationToken);

            if (term == null)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermNotFound),
                    request.TermId.Value);
                return Result<GetUniAdminStudentEvaluationsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentEvaluationsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermAccessDenied),
                    ResultErrorType.Forbidden);
            }
        }
        else
        {
            term = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .Where(t => t.UniversityId == universityId && t.Status == TermStatus.Open)
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (term == null)
                return Result<GetUniAdminStudentEvaluationsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound),
                    ResultErrorType.NotFound);
        }

        // Verify student belongs to this term
        var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(st =>
                st.TermId == term.TermId
                && st.StudentId == request.StudentId
                && st.EnrollmentStatus == EnrollmentStatus.Active
                && st.DeletedAt == null,
                cancellationToken);

        if (studentTerm == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogStudentNotFound),
                request.StudentId, term.TermId, universityId);
            return Result<GetUniAdminStudentEvaluationsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotFound),
                ResultErrorType.NotFound);
        }

        // Find student's InternshipGroup for this term
        InternshipStudent? internStudent = null;
        if (studentTerm.EnterpriseId.HasValue)
        {
            internStudent = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Where(isv =>
                    isv.StudentId == request.StudentId
                    && isv.InternshipGroup.EnterpriseId == studentTerm.EnterpriseId
                    && isv.DeletedAt == null)
                .OrderByDescending(isv => isv.JoinedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (internStudent == null)
        {
            // No group — no evaluations
            return Result<GetUniAdminStudentEvaluationsResponse>.Success(
                new GetUniAdminStudentEvaluationsResponse(),
                _messageService.GetMessage(MessageKeys.UniAdminInternship.EvaluationsRetrieved));
        }

        // Load published evaluations for this student in their group
        var evaluations = await _unitOfWork.Repository<Evaluation>().Query()
            .Include(e => e.Cycle)
            .Include(e => e.Evaluator)
            .Include(e => e.Details)
                .ThenInclude(d => d.Criteria)
            .AsNoTracking()
            .Where(e =>
                e.InternshipId == internStudent.InternshipId
                && e.StudentId == request.StudentId
                && e.Status == EvaluationStatus.Published
                && e.DeletedAt == null)
            .OrderBy(e => e.Cycle.StartDate)
            .ToListAsync(cancellationToken);

        var cycles = evaluations.Select(e => new EvaluationCycleDto
        {
            EvaluationId = e.EvaluationId,
            CycleId = e.CycleId,
            CycleName = e.Cycle.Name,
            CycleStartDate = e.Cycle.StartDate,
            CycleEndDate = e.Cycle.EndDate,
            EvaluationStatus = e.Status,
            EvaluatorName = e.Evaluator.FullName,
            TotalScore = e.TotalScore,
            GeneralComment = e.Note,
            PublishedAt = e.UpdatedAt ?? e.CreatedAt,
            Details = e.Details.Select(d => new EvaluationDetailDto
            {
                CriteriaName = d.Criteria.Name,
                CriteriaDescription = d.Criteria.Description,
                MaxScore = d.Criteria.MaxScore,
                Weight = d.Criteria.Weight,
                Score = d.Score,
                WeightedScore = d.Score * d.Criteria.Weight,
                Comment = d.Comment
            }).ToList()
        }).ToList();

        var averageScore = cycles.Count > 0 && cycles.Any(c => c.TotalScore.HasValue)
            ? cycles.Where(c => c.TotalScore.HasValue).Average(c => c.TotalScore!.Value)
            : (decimal?)null;

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetEvaluations),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentEvaluationsResponse>.Success(
            new GetUniAdminStudentEvaluationsResponse
            {
                TotalCycles = cycles.Count,
                AverageScore = averageScore.HasValue ? Math.Round(averageScore.Value, 2) : null,
                Cycles = cycles
            },
            _messageService.GetMessage(MessageKeys.UniAdminInternship.EvaluationsRetrieved));
    }
}

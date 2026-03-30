using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;

public class GetUniAdminStudentDetailHandler
    : IRequestHandler<GetUniAdminStudentDetailQuery, Result<GetUniAdminStudentDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentDetailHandler> _logger;

    public GetUniAdminStudentDetailHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentDetailHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentDetailResponse>> Handle(
        GetUniAdminStudentDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentDetailResponse>.Failure(
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
            return Result<GetUniAdminStudentDetailResponse>.Failure(
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
                return Result<GetUniAdminStudentDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentDetailResponse>.Failure(
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
                return Result<GetUniAdminStudentDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound),
                    ResultErrorType.NotFound);
        }

        // Load StudentTerm (verify student belongs to this term+university)
        var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
            .Include(st => st.Student)
                .ThenInclude(s => s.User)
            .Include(st => st.Enterprise)
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
            return Result<GetUniAdminStudentDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotFound),
                ResultErrorType.NotFound);
        }

        var student = studentTerm.Student;
        var user = student.User;

        // Load InternshipStudent (if student has been placed at an enterprise)
        InternshipStudent? internStudent = null;
        if (studentTerm.EnterpriseId.HasValue)
        {
            internStudent = await _unitOfWork.Repository<InternshipStudent>().Query()
                .Include(isv => isv.InternshipGroup)
                    .ThenInclude(ig => ig.Mentor!)
                        .ThenInclude(m => m.User)
                .AsNoTracking()
                .Where(isv =>
                    isv.StudentId == request.StudentId
                    && isv.InternshipGroup.EnterpriseId == studentTerm.EnterpriseId
                    && isv.DeletedAt == null)
                .OrderByDescending(isv => isv.JoinedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var group = internStudent?.InternshipGroup;
        var hasGroup = group != null;

        // Check for pending application
        var hasPendingApp = await _unitOfWork.Repository<InternshipApplication>().Query()
            .AsNoTracking()
            .AnyAsync(app =>
                app.StudentId == request.StudentId
                && app.TermId == term.TermId
                && app.DeletedAt == null
                && (app.Status == InternshipApplicationStatus.Applied
                    || app.Status == InternshipApplicationStatus.Interviewing
                    || app.Status == InternshipApplicationStatus.Offered
                    || app.Status == InternshipApplicationStatus.PendingAssignment),
                cancellationToken);

        var uiStatus = DeriveUiStatus(studentTerm.PlacementStatus, hasGroup, hasPendingApp, term.Status);

        // Logbook count
        var logbookCount = 0;
        var weeklyLogbooks = new List<UniAdminWeeklyLogbookDto>();
        if (internStudent != null && group != null)
        {
            var studentLogbooks = await _unitOfWork.Repository<Logbook>().Query()
                .AsNoTracking()
                .Where(l =>
                    l.InternshipId == internStudent.InternshipId
                    && l.StudentId == request.StudentId
                    && l.DeletedAt == null)
                .OrderBy(l => l.DateReport)
                .ToListAsync(cancellationToken);

            logbookCount = studentLogbooks.Count;
            weeklyLogbooks = BuildWeeklyLogbooks(studentLogbooks, group.StartDate);
        }

        var logbookSummary = CalculateLogbookSummary(internStudent, group, logbookCount);

        // Violation count
        var violationCount = group != null
            ? await _unitOfWork.Repository<ViolationReport>().Query()
                .AsNoTracking()
                .CountAsync(v =>
                    v.StudentId == request.StudentId
                    && v.InternshipGroupId == group.InternshipId
                    && v.DeletedAt == null,
                    cancellationToken)
            : 0;

        // Published evaluation count
        var publishedEvalCount = 0;
        if (group != null)
        {
            publishedEvalCount = await _unitOfWork.Repository<Evaluation>().Query()
                .AsNoTracking()
                .CountAsync(e =>
                    e.InternshipId == group.InternshipId
                    && e.StudentId == request.StudentId
                    && e.Status == EvaluationStatus.Published
                    && e.DeletedAt == null,
                    cancellationToken);
        }

        var response = new GetUniAdminStudentDetailResponse
        {
            StudentId = student.StudentId,
            StudentCode = user.UserCode,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            ClassName = student.ClassName,
            Major = student.Major,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            ResolvedTermId = term.TermId,
            TermName = term.Name,
            TermStartDate = term.StartDate,
            TermEndDate = term.EndDate,
            InternshipStatus = uiStatus,
            EnterpriseId = studentTerm.EnterpriseId,
            EnterpriseName = studentTerm.Enterprise?.Name,
            EnterprisePosition = group?.Description,
            MentorId = group?.MentorId,
            MentorName = group?.Mentor?.User?.FullName,
            MentorEmail = group?.Mentor?.User?.Email,
            Logbook = logbookSummary,
            LogbookWeeks = weeklyLogbooks,
            ViolationCount = violationCount,
            PublishedEvaluationCount = publishedEvalCount
        };

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetStudentDetail),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentDetailResponse>.Success(
            response,
            _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentDetailRetrieved));
    }

    private static InternshipUiStatus DeriveUiStatus(
        PlacementStatus placementStatus,
        bool hasGroup,
        bool hasPendingApp,
        TermStatus termStatus)
    {
        if (placementStatus == PlacementStatus.Unplaced)
            return hasPendingApp ? InternshipUiStatus.PendingConfirmation : InternshipUiStatus.Unplaced;

        if (termStatus == TermStatus.Closed)
            return InternshipUiStatus.Completed;

        return hasGroup ? InternshipUiStatus.Active : InternshipUiStatus.NoGroup;
    }

    private static LogbookSummaryDto? CalculateLogbookSummary(
        InternshipStudent? internStudent,
        InternshipGroup? group,
        int submittedCount)
    {
        if (internStudent == null || group == null)
            return null;

        var joinedAt = internStudent.JoinedAt.Date;
        var phaseEnd = group.EndDate?.Date ?? DateTime.UtcNow.Date;
        var effectiveEnd = DateTime.UtcNow.Date < phaseEnd ? DateTime.UtcNow.Date : phaseEnd;

        if (joinedAt > effectiveEnd)
            return new LogbookSummaryDto { Missing = 0, Submitted = submittedCount, Total = 0, PercentComplete = 0 };

        var total = CountBusinessDays(joinedAt, effectiveEnd);
        var missing = Math.Max(0, total - submittedCount);
        var percent = total > 0 ? (int)Math.Round((double)submittedCount / total * 100) : 0;

        return new LogbookSummaryDto
        {
            Missing = missing,
            Submitted = submittedCount,
            Total = total,
            PercentComplete = percent
        };
    }

    private static int CountBusinessDays(DateTime start, DateTime end)
    {
        int count = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static List<UniAdminWeeklyLogbookDto> BuildWeeklyLogbooks(
        List<Logbook> logbooks,
        DateTime? groupStartDate)
    {
        if (logbooks.Count == 0)
            return new List<UniAdminWeeklyLogbookDto>();

        var anchor = GetStartOfWeek((groupStartDate ?? logbooks.Min(x => x.DateReport)).Date);

        return logbooks
            .GroupBy(x => GetWeekNumber(x.DateReport, anchor))
            .OrderBy(x => x.Key)
            .Select(g =>
            {
                var weekStart = anchor.AddDays((g.Key - 1) * 7);
                var weekEnd = weekStart.AddDays(4);

                var entries = g
                    .OrderBy(x => x.DateReport)
                    .Select(x => new UniAdminWeeklyLogbookEntryDto
                    {
                        LogbookId = x.LogbookId,
                        DateReport = x.DateReport,
                        Summary = x.Summary,
                        Issue = x.Issue,
                        Plan = x.Plan,
                        Status = x.Status,
                        StatusBadge = x.Status == LogbookStatus.LATE ? "Late" : "Submitted"
                    })
                    .ToList();

                var submittedCount = entries.Count(x => x.Status != LogbookStatus.LATE);
                var lateCount = entries.Count(x => x.Status == LogbookStatus.LATE);
                var totalCount = entries.Count;

                return new UniAdminWeeklyLogbookDto
                {
                    WeekNumber = g.Key,
                    WeekTitle = $"Week {g.Key}: {GetWeekTheme(g.Key)}",
                    WeekStartDate = weekStart,
                    WeekEndDate = weekEnd,
                    SubmittedCount = submittedCount,
                    LateCount = lateCount,
                    TotalCount = totalCount,
                    CompletionRatio = $"{submittedCount}/{totalCount}",
                    Entries = entries
                };
            })
            .ToList();
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    private static int GetWeekNumber(DateTime date, DateTime anchor)
    {
        return ((date.Date - anchor).Days / 7) + 1;
    }

    private static string GetWeekTheme(int weekNumber)
    {
        return weekNumber switch
        {
            1 => "Kickoff & Research",
            2 => "Implementation & Testing",
            3 => "Optimization & Stabilization",
            4 => "Release & Handover",
            _ => "Execution"
        };
    }
}

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
            var studentExists = await _unitOfWork.Repository<Student>().Query()
                .AsNoTracking()
                .AnyAsync(s => s.StudentId == request.StudentId && s.DeletedAt == null, cancellationToken);

            if (studentExists)
            {
                var belongsToAnotherUniversity = await _unitOfWork.Repository<StudentTerm>().Query()
                    .AsNoTracking()
                    .AnyAsync(st =>
                        st.StudentId == request.StudentId
                        && st.EnrollmentStatus == EnrollmentStatus.Active
                        && st.DeletedAt == null
                        && st.Term.UniversityId != universityId,
                        cancellationToken);

                if (belongsToAnotherUniversity)
                {
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.LogStudentNotInUniversity),
                        currentUserId, term.TermId, universityId);
                    return Result<GetUniAdminStudentDetailResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotInUniversity),
                        ResultErrorType.Forbidden);
                }
            }

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
                && UniAdminInternshipRules.IsPendingApplicationStatus(app.Status),
                cancellationToken);

        var uiStatus = UniAdminInternshipRules.DeriveUiStatus(studentTerm.PlacementStatus, group, hasPendingApp);

        // Logbook count
        var weeklyLogbooks = new List<UniAdminWeeklyLogbookDto>();
        List<DateTime> submittedDates = new();
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

            submittedDates = studentLogbooks
                .Where(x => UniAdminInternshipRules.IsSubmittedLogbookStatus(x.Status))
                .Select(x => x.DateReport)
                .ToList();

            var today = DateTime.UtcNow.Date;
            var joinedAt = internStudent.JoinedAt.Date;
            var phaseEnd = group.EndDate?.Date ?? today;
            var effectiveEnd = today < phaseEnd ? today : phaseEnd;

            weeklyLogbooks = BuildWeeklyLogbooks(studentLogbooks, joinedAt, effectiveEnd);
        }

        var logbookSummary = UniAdminInternshipRules.CalculateLogbookSummary(internStudent, group, submittedDates);

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
            InternshipPhaseStartDate = group?.StartDate,
            InternshipPhaseEndDate = group?.EndDate,
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

    private List<UniAdminWeeklyLogbookDto> BuildWeeklyLogbooks(
        List<Logbook> logbooks,
        DateTime windowStart,
        DateTime windowEnd)
    {
        if (windowStart > windowEnd)
            return new List<UniAdminWeeklyLogbookDto>();

        var logbookByDate = logbooks
            .GroupBy(x => x.DateReport.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

        var weeks = new List<UniAdminWeeklyLogbookDto>();
        var currentWeekStart = windowStart.Date;
        var weekNumber = 1;
        var today = DateTime.UtcNow.Date;

        while (currentWeekStart <= windowEnd.Date)
        {
            var currentWeekEnd = EndOfWeekSunday(currentWeekStart);
            if (currentWeekEnd > windowEnd.Date)
                currentWeekEnd = windowEnd.Date;

            var entries = new List<UniAdminWeeklyLogbookEntryDto>();
            var submittedCount = 0;
            var lateCount = 0;
            var requiredCount = 0;

            for (var date = currentWeekStart; date <= currentWeekEnd; date = date.AddDays(1))
            {
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                if (!isWeekend)
                    requiredCount++;

                logbookByDate.TryGetValue(date, out var existingLogbook);

                if (existingLogbook != null && UniAdminInternshipRules.IsSubmittedLogbookStatus(existingLogbook.Status) && !isWeekend)
                {
                    submittedCount++;
                    if (existingLogbook.Status == LogbookStatus.LATE)
                        lateCount++;
                }

                var statusBadge = isWeekend
                    ? "Off"
                    : existingLogbook != null
                        ? existingLogbook.Status == LogbookStatus.LATE ? "SubmittedLate" : "Submitted"
                        : date < today ? "Missing" : "Pending";

                entries.Add(new UniAdminWeeklyLogbookEntryDto
                {
                    LogbookId = existingLogbook?.LogbookId,
                    DateReport = date,
                    Summary = existingLogbook?.Summary ?? string.Empty,
                    Issue = existingLogbook?.Issue,
                    Plan = existingLogbook?.Plan ?? string.Empty,
                    Status = existingLogbook?.Status,
                    StatusBadge = statusBadge
                });
            }

            weeks.Add(new UniAdminWeeklyLogbookDto
            {
                WeekNumber = weekNumber,
                WeekTitle = string.Format(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.WeekTitleFormat),
                    weekNumber,
                    currentWeekStart.ToString("dd/MM"),
                    currentWeekEnd.ToString("dd/MM/yyyy")),
                WeekStartDate = currentWeekStart,
                WeekEndDate = currentWeekEnd,
                SubmittedCount = submittedCount,
                LateCount = lateCount,
                TotalCount = requiredCount,
                CompletionRatio = $"{submittedCount}/{requiredCount}",
                Entries = entries
            });

            currentWeekStart = currentWeekEnd.AddDays(1);
            weekNumber++;
        }

        return weeks;
    }

    private static DateTime EndOfWeekSunday(DateTime date)
    {
        var diff = DayOfWeek.Sunday - date.DayOfWeek;
        return date.Date.AddDays(diff < 0 ? diff + 7 : diff);
    }
}

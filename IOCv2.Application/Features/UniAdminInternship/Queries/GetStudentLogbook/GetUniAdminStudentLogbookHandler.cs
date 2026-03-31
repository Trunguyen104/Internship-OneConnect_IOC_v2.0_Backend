using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbook;

public class GetUniAdminStudentLogbookHandler
    : IRequestHandler<GetUniAdminStudentLogbookQuery, Result<GetUniAdminStudentLogbookResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentLogbookHandler> _logger;

    public GetUniAdminStudentLogbookHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentLogbookHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentLogbookResponse>> Handle(
        GetUniAdminStudentLogbookQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentLogbookResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

        if (universityUser == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound),
                currentUserId);
            return Result<GetUniAdminStudentLogbookResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                ResultErrorType.Forbidden);
        }

        var universityId = universityUser.UniversityId;

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
                return Result<GetUniAdminStudentLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentLogbookResponse>.Failure(
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
                return Result<GetUniAdminStudentLogbookResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound),
                    ResultErrorType.NotFound);
        }

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
                    return Result<GetUniAdminStudentLogbookResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotInUniversity),
                        ResultErrorType.Forbidden);
                }
            }

            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogStudentNotFound),
                request.StudentId, term.TermId, universityId);
            return Result<GetUniAdminStudentLogbookResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotFound),
                ResultErrorType.NotFound);
        }

        InternshipStudent? internStudent = null;
        if (studentTerm.EnterpriseId.HasValue)
        {
            internStudent = await _unitOfWork.Repository<InternshipStudent>().Query()
                .Include(isv => isv.InternshipGroup)
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
            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetLogbook),
                currentUserId, request.StudentId, term.TermId);
            return Result<GetUniAdminStudentLogbookResponse>.Success(
                new GetUniAdminStudentLogbookResponse
                {
                    ResolvedTermId = term.TermId,
                    HasInternshipGroup = false,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalWeeks = 0,
                    Weeks = new List<UniAdminLogbookWeekDto>()
                },
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogbookRetrieved));
        }

        var group = internStudent.InternshipGroup;
        var studentLogbooks = await _unitOfWork.Repository<Logbook>().Query()
            .AsNoTracking()
            .Where(l =>
                l.InternshipId == internStudent.InternshipId
                && l.StudentId == request.StudentId
                && l.DeletedAt == null)
            .OrderBy(l => l.DateReport)
            .ThenBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var submittedDates = studentLogbooks
            .Where(x => UniAdminInternshipRules.IsSubmittedLogbookStatus(x.Status))
            .Select(x => x.DateReport)
            .ToList();

        var summary = UniAdminInternshipRules.CalculateLogbookSummary(internStudent, group, submittedDates);
        var windowStart = internStudent.JoinedAt.Date;
        var today = DateTime.UtcNow.Date;
        var phaseEnd = group.EndDate?.Date ?? today;
        var windowEnd = today < phaseEnd ? today : phaseEnd;

        var allWeeks = BuildWeeks(studentLogbooks, windowStart, windowEnd);
        var pagedWeeks = allWeeks
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetLogbook),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentLogbookResponse>.Success(
            new GetUniAdminStudentLogbookResponse
            {
                ResolvedTermId = term.TermId,
                HasInternshipGroup = true,
                Summary = summary,
                TotalWeeks = allWeeks.Count,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Weeks = pagedWeeks
            },
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogbookRetrieved));
    }

    private static List<UniAdminLogbookWeekDto> BuildWeeks(
        List<Logbook> logbooks,
        DateTime windowStart,
        DateTime windowEnd)
    {
        if (windowStart > windowEnd)
            return new List<UniAdminLogbookWeekDto>();

        var byDate = logbooks
            .GroupBy(x => x.DateReport.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

        var weeks = new List<UniAdminLogbookWeekDto>();
        var today = DateTime.UtcNow.Date;
        var currentWeekStart = windowStart;
        var weekNumber = 1;

        while (currentWeekStart <= windowEnd)
        {
            var currentWeekEnd = EndOfWeekSunday(currentWeekStart);
            if (currentWeekEnd > windowEnd)
                currentWeekEnd = windowEnd;

            var days = new List<UniAdminLogbookDayDto>();
            var requiredCount = 0;
            var submittedCount = 0;

            for (var date = currentWeekStart; date <= currentWeekEnd; date = date.AddDays(1))
            {
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var isRequired = !isWeekend;
                if (isRequired)
                    requiredCount++;

                byDate.TryGetValue(date, out var logbook);
                var isSubmitted = logbook != null && UniAdminInternshipRules.IsSubmittedLogbookStatus(logbook.Status);
                var isLate = logbook?.Status == LogbookStatus.LATE;

                if (isRequired && isSubmitted)
                    submittedCount++;

                var isPastDueMissing = isRequired && !isSubmitted && date < today;
                var isPendingMissing = isRequired && !isSubmitted && date >= today;

                var badge = isWeekend
                    ? "Off"
                    : isSubmitted
                        ? isLate ? "SubmittedLate" : "Submitted"
                        : isPastDueMissing ? "Missing" : "Pending";

                days.Add(new UniAdminLogbookDayDto
                {
                    LogbookId = logbook?.LogbookId,
                    Date = date,
                    IsWeekend = isWeekend,
                    IsRequired = isRequired,
                    IsSubmitted = isSubmitted,
                    IsLate = isLate,
                    IsPastDueMissing = isPastDueMissing,
                    IsPendingMissing = isPendingMissing,
                    LogbookStatus = logbook?.Status,
                    StatusBadge = badge,
                    SubmittedAt = logbook?.CreatedAt,
                    Summary = logbook?.Summary,
                    Issue = logbook?.Issue,
                    Plan = logbook?.Plan
                });
            }

            weeks.Add(new UniAdminLogbookWeekDto
            {
                WeekNumber = weekNumber,
                DateFrom = currentWeekStart,
                DateTo = currentWeekEnd,
                IsCurrentWeek = currentWeekStart <= today && today <= currentWeekEnd,
                RequiredCount = requiredCount,
                SubmittedCount = submittedCount,
                MissingCount = Math.Max(0, requiredCount - submittedCount),
                Days = days
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



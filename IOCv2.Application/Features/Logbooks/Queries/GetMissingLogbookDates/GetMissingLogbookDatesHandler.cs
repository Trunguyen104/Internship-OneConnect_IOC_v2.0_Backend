using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Logbooks.Queries.GetMissingLogbookDates;

/// <summary>
/// Calculates the list of working days (Mon–Fri, non-holiday) on which a student
/// did NOT submit a logbook entry, from the start of their active internship phase
/// up to (and including) today UTC.
/// </summary>
public class GetMissingLogbookDatesHandler
    : IRequestHandler<GetMissingLogbookDatesQuery, Result<GetMissingLogbookDatesResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetMissingLogbookDatesHandler> _logger;

    public GetMissingLogbookDatesHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetMissingLogbookDatesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetMissingLogbookDatesResponse>> Handle(
        GetMissingLogbookDatesQuery request,
        CancellationToken cancellationToken)
    {
        // ── 1. Resolve the target StudentId ───────────────────────────────────
        Guid studentId;

        if (request.StudentId.HasValue)
        {
            studentId = request.StudentId.Value;
        }
        else
        {
            // Fall back to the currently authenticated user
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                _logger.LogWarning("GetMissingLogbookDates: invalid or missing UserId in token.");
                return Result<GetMissingLogbookDatesResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Logbooks.StudentNotFound),
                    ResultErrorType.Unauthorized);
            }

            // Resolve Student from UserId
            var student = await _unitOfWork.Repository<Student>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

            if (student is null)
            {
                _logger.LogWarning("GetMissingLogbookDates: no Student record for UserId {UserId}.", currentUserId);
                return Result<GetMissingLogbookDatesResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Logbooks.StudentNotFound),
                    ResultErrorType.NotFound);
            }

            studentId = student.StudentId;
        }

        _logger.LogInformation("GetMissingLogbookDates: calculating for StudentId={StudentId}.", studentId);

        // ── 2. Find the student's active InternshipGroup (via InternshipStudent) ──
        //      Query from InternshipGroup side to ensure InternshipPhase is always loaded.
        var internshipStudent = await _unitOfWork.Repository<InternshipStudent>()
            .Query()
            .AsNoTracking()
            .Where(i => i.StudentId == studentId
                        && i.InternshipGroup.Status == GroupStatus.Active)
            .Include(i => i.InternshipGroup)
                .ThenInclude(g => g.InternshipPhase)
            .OrderByDescending(i => i.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (internshipStudent is null || internshipStudent.InternshipGroup is null)
        {
            _logger.LogWarning("GetMissingLogbookDates: no active InternshipGroup for StudentId={StudentId}.", studentId);
            return Result<GetMissingLogbookDatesResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Logbooks.NoActiveInternship),
                ResultErrorType.NotFound);
        }

        var group = internshipStudent.InternshipGroup;
        var phase = group.InternshipPhase;

        // Guard: InternshipPhase must be loaded (should always be via ThenInclude)
        if (phase is null)
        {
            _logger.LogWarning(
                "GetMissingLogbookDates: InternshipPhase not loaded for GroupId={GroupId}, StudentId={StudentId}.",
                group.InternshipId, studentId);
            return Result<GetMissingLogbookDatesResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Logbooks.NoActiveInternship),
                ResultErrorType.NotFound);
        }

        // Determine effective start date
        // group.StartDate is DateTime? — guard against default(DateTime) which maps to year 0001
        DateOnly startDate;
        if (group.StartDate.HasValue && group.StartDate.Value > DateTime.MinValue)
            startDate = DateOnly.FromDateTime(group.StartDate.Value);
        else
            startDate = phase.StartDate;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Guard: if start date is in the future, there are no missing dates yet
        if (startDate > today)
        {
            _logger.LogInformation(
                "GetMissingLogbookDates: internship starts in the future ({Start}) for StudentId={StudentId}.",
                startDate, studentId);

            return Result<GetMissingLogbookDatesResponse>.Success(new GetMissingLogbookDatesResponse
            {
                StudentId        = studentId,
                InternshipStartDate = startDate,
                CalculatedUpTo   = today,
                TotalWorkingDays = 0,
                SubmittedDays    = 0,
                MissingDates     = new List<DateOnly>()
            });
        }

        // ── 3. Fetch the dates already submitted by this student in the window ──
        // Convert boundaries to UTC DateTime — Npgsql requires DateTimeKind.Utc for timestamptz columns
        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var todayDateTime = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddDays(1); // exclusive upper bound

        var rawDates = await _unitOfWork.Repository<Logbook>()
            .Query()
            .AsNoTracking()
            .Where(l => l.StudentId == studentId
                        && l.DateReport >= startDateTime
                        && l.DateReport < todayDateTime)
            .Select(l => l.DateReport)
            .ToListAsync(cancellationToken);

        var submittedDates = rawDates
            .Select(d => DateOnly.FromDateTime(d.Date))
            .Distinct()
            .ToList();

        var submittedSet = new HashSet<DateOnly>(submittedDates);

        // ── 4. Fetch public holidays in the window ────────────────────────────
        // Load ALL holidays (table is small) then filter in-memory to avoid
        // potential DateOnly-in-SQL translation issues with some Npgsql versions
        var allHolidayDates = await _unitOfWork.Repository<PublicHoliday>()
            .Query()
            .AsNoTracking()
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        var holidaySet = new HashSet<DateOnly>(
            allHolidayDates.Where(d => d >= startDate && d <= today));

        // ── 5. Iterate over each day in [startDate, today] ───────────────────
        var missingDates    = new List<DateOnly>();
        int totalWorkingDays = 0;

        for (var current = startDate; current <= today; current = current.AddDays(1))
        {
            // Skip weekends
            if (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            // Skip public holidays
            if (holidaySet.Contains(current))
                continue;

            totalWorkingDays++;

            // If no logbook was submitted on this working day → it's missing
            if (!submittedSet.Contains(current))
                missingDates.Add(current);
        }

        _logger.LogInformation(
            "GetMissingLogbookDates: StudentId={StudentId} — {Total} working days, {Submitted} submitted, {Missing} missing.",
            studentId, totalWorkingDays, submittedSet.Count, missingDates.Count);

        var response = new GetMissingLogbookDatesResponse
        {
            StudentId           = studentId,
            InternshipStartDate = startDate,
            CalculatedUpTo      = today,
            TotalWorkingDays    = totalWorkingDays,
            SubmittedDays       = submittedSet.Count,
            MissingDates        = missingDates
        };

        return Result<GetMissingLogbookDatesResponse>.Success(response);
    }
}

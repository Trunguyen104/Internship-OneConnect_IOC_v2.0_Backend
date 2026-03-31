using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentList;

public class GetUniAdminStudentListHandler
    : IRequestHandler<GetUniAdminStudentListQuery, Result<GetUniAdminStudentListResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentListHandler> _logger;

    public GetUniAdminStudentListHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentListHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentListResponse>> Handle(
        GetUniAdminStudentListQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Parse current user
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentListResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        // 2. Get UniversityId
        var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

        if (universityUser == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound),
                currentUserId);
            return Result<GetUniAdminStudentListResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                ResultErrorType.Forbidden);
        }

        var universityId = universityUser.UniversityId;

        // 3. Resolve Term
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
                return Result<GetUniAdminStudentListResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentListResponse>.Failure(
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
            {
                _logger.LogInformation(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogNoOpenTerm),
                    universityId);
                var emptyResp = new GetUniAdminStudentListResponse
                {
                    Students = new PaginatedResult<StudentListItemDto>(
                        new List<StudentListItemDto>(), 0, request.PageNumber, request.PageSize),
                    Summary = new SummaryCardsDto(),
                    ResolvedTermId = Guid.Empty
                };
                return Result<GetUniAdminStudentListResponse>.Success(
                    emptyResp,
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound));
            }
        }

        var resolvedTermId = term.TermId;

        // 4. Load StudentTerms
        var initialResolvedTermId = resolvedTermId;
        var studentTerms = await _unitOfWork.Repository<StudentTerm>().Query()
            .Include(st => st.Student)
                .ThenInclude(s => s.User)
            .Include(st => st.Enterprise)
            .AsNoTracking()
            .Where(st => st.TermId == initialResolvedTermId
                      && st.EnrollmentStatus == EnrollmentStatus.Active
                      && st.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // If client does not pin a term and the current open term has no data,
        // fallback to the latest term (same university) that has active student enrollments.
        if (!request.TermId.HasValue && !studentTerms.Any())
        {
            var fallbackTerm = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .Where(t => t.UniversityId == universityId)
                .Where(t => _unitOfWork.Repository<StudentTerm>().Query()
                    .Any(st => st.TermId == t.TermId
                               && st.EnrollmentStatus == EnrollmentStatus.Active
                               && st.DeletedAt == null))
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (fallbackTerm != null && fallbackTerm.TermId != resolvedTermId)
            {
                resolvedTermId = fallbackTerm.TermId;

                studentTerms = await _unitOfWork.Repository<StudentTerm>().Query()
                    .Include(st => st.Student)
                        .ThenInclude(s => s.User)
                    .Include(st => st.Enterprise)
                    .AsNoTracking()
                    .Where(st => st.TermId == resolvedTermId
                              && st.EnrollmentStatus == EnrollmentStatus.Active
                              && st.DeletedAt == null)
                    .ToListAsync(cancellationToken);
            }
        }

        if (!studentTerms.Any())
        {
            var emptyResp = new GetUniAdminStudentListResponse
            {
                Students = new PaginatedResult<StudentListItemDto>(
                    new List<StudentListItemDto>(), 0, request.PageNumber, request.PageSize),
                Summary = new SummaryCardsDto(),
                ResolvedTermId = resolvedTermId
            };
            return Result<GetUniAdminStudentListResponse>.Success(
                emptyResp,
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentsRetrieved));
        }

        var studentIds = studentTerms.Select(st => st.StudentId).ToList();
        var studentTermDict = studentTerms.ToDictionary(st => st.StudentId);

        // 5. Load InternshipStudents (bulk, avoid N+1)
        var internshipStudents = await _unitOfWork.Repository<InternshipStudent>().Query()
            .Include(isv => isv.InternshipGroup)
                .ThenInclude(ig => ig.Mentor!)
                    .ThenInclude(m => m.User)
            .AsNoTracking()
            .Where(isv => studentIds.Contains(isv.StudentId) && isv.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Match each InternshipStudent to StudentTerm by EnterpriseId.
        // If historical rows exist, keep the latest JoinedAt to avoid non-deterministic mapping.
        var internStudentDict = new Dictionary<Guid, InternshipStudent>();
        foreach (var isv in internshipStudents)
        {
            if (!studentTermDict.TryGetValue(isv.StudentId, out var st)) continue;
            if (!st.EnterpriseId.HasValue) continue;
            if (isv.InternshipGroup?.EnterpriseId != st.EnterpriseId) continue;

            if (!internStudentDict.TryGetValue(isv.StudentId, out var existing)
                || isv.JoinedAt > existing.JoinedAt)
            {
                internStudentDict[isv.StudentId] = isv;
            }
        }

        // 6. Load Logbook counts (single query)
        var groupIds = internStudentDict.Values
            .Select(isv => isv.InternshipId)
            .Distinct()
            .ToList();

        var logbookCountDict = new Dictionary<(Guid InternshipId, Guid StudentId), int>();
        if (groupIds.Any())
        {
            var rawCounts = await _unitOfWork.Repository<Logbook>().Query()
                .AsNoTracking()
                .Where(l => groupIds.Contains(l.InternshipId)
                         && l.StudentId.HasValue
                         && studentIds.Contains(l.StudentId!.Value)
                         && l.DeletedAt == null)
                .GroupBy(l => new { l.InternshipId, StudentId = l.StudentId!.Value })
                .Select(g => new { g.Key.InternshipId, g.Key.StudentId, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var item in rawCounts)
                logbookCountDict[(item.InternshipId, item.StudentId)] = item.Count;
        }

        // 7. Load ViolationReport counts scoped to selected internship group
        var violationCountDict = new Dictionary<(Guid StudentId, Guid InternshipId), int>();
        if (groupIds.Any())
        {
            var violationCountList = await _unitOfWork.Repository<ViolationReport>().Query()
                .AsNoTracking()
                .Where(v => studentIds.Contains(v.StudentId)
                         && groupIds.Contains(v.InternshipGroupId)
                         && v.DeletedAt == null)
                .GroupBy(v => new { v.StudentId, v.InternshipGroupId })
                .Select(g => new { g.Key.StudentId, g.Key.InternshipGroupId, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var item in violationCountList)
                violationCountDict[(item.StudentId, item.InternshipGroupId)] = item.Count;
        }

        // 8. Load active InternshipApplications
        var activeApplications = await _unitOfWork.Repository<InternshipApplication>().Query()
            .AsNoTracking()
            .Where(app => studentIds.Contains(app.StudentId)
                       && app.TermId == resolvedTermId
                       && app.DeletedAt == null
                       && (app.Status == InternshipApplicationStatus.Applied
                           || app.Status == InternshipApplicationStatus.Interviewing
                           || app.Status == InternshipApplicationStatus.Offered
                           || app.Status == InternshipApplicationStatus.PendingAssignment))
            .ToListAsync(cancellationToken);

        var pendingAppStudentIds = activeApplications.Select(a => a.StudentId).ToHashSet();
        var activeApplicationByStudent = activeApplications
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        // 9. Build list items
        var allItems = new List<StudentListItemDto>();
        var placedCount = 0;
        var unplacedCount = 0;
        var noMentorCount = 0;
        foreach (var st in studentTerms)
        {
            var student = st.Student;
            var user = student?.User;
            if (student == null || user == null) continue;

            internStudentDict.TryGetValue(st.StudentId, out var internStudent);
            var group = internStudent?.InternshipGroup;
            var hasGroup = internStudent != null && group != null;
            var hasPendingApp = pendingAppStudentIds.Contains(st.StudentId);

            var uiStatus = DeriveUiStatus(st.PlacementStatus, hasGroup, hasPendingApp, group?.EndDate);

            var logbookCount = (internStudent != null && group != null
                && logbookCountDict.TryGetValue((internStudent.InternshipId, st.StudentId), out var lc))
                ? lc : 0;

            var logbookSummary = CalculateLogbookSummary(internStudent, group, logbookCount);
            var vioCount = internStudent != null
                && violationCountDict.TryGetValue((st.StudentId, internStudent.InternshipId), out var scopedCount)
                ? scopedCount
                : 0;

            activeApplicationByStudent.TryGetValue(st.StudentId, out var activeApp);

            if (st.PlacementStatus == PlacementStatus.Placed)
            {
                placedCount++;
                if (hasGroup && group?.MentorId == null)
                    noMentorCount++;
            }
            else
            {
                unplacedCount++;
            }

            allItems.Add(new StudentListItemDto
            {
                StudentId = st.StudentId,
                StudentCode = user.UserCode,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                ClassName = student.ClassName,
                Major = student.Major,
                EnterpriseId = st.EnterpriseId,
                EnterpriseName = st.Enterprise?.Name,
                MentorName = group?.Mentor?.User?.FullName,
                Logbook = logbookSummary,
                InternshipStatus = uiStatus,
                ApplicationSource = activeApp?.Source.ToString(),
                ViolationCount = vioCount
            });
        }

        // 10. SummaryCards from full list (before filters)
        var summary = new SummaryCardsDto
        {
            TotalStudents = allItems.Count,
            Placed = placedCount,
            Unplaced = unplacedCount,
            NoMentor = noMentorCount
        };

        // 11. Apply filters in-memory
        var filtered = allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var lower = request.SearchTerm.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.FullName.ToLowerInvariant().Contains(lower)
                || i.StudentCode.ToLowerInvariant().Contains(lower)
                || (i.EnterpriseName?.ToLowerInvariant().Contains(lower) ?? false));
        }

        if (request.EnterpriseId.HasValue)
            filtered = filtered.Where(i => i.EnterpriseId == request.EnterpriseId.Value);

        if (request.Status.HasValue)
            filtered = filtered.Where(i => i.InternshipStatus == request.Status.Value);

        if (request.LogbookStatus.HasValue)
        {
            filtered = request.LogbookStatus.Value switch
            {
                LogbookFilterStatus.Sufficient => filtered.Where(i => i.Logbook != null && i.Logbook.PercentComplete >= 75),
                LogbookFilterStatus.SlightlyMissing => filtered.Where(i => i.Logbook != null && i.Logbook.PercentComplete >= 50 && i.Logbook.PercentComplete < 75),
                LogbookFilterStatus.MissingMany => filtered.Where(i => i.Logbook != null && i.Logbook.PercentComplete < 50),
                _ => filtered
            };
        }

        // 12. Sorting
        filtered = (request.SortBy.ToLowerInvariant(), request.SortOrder.ToLowerInvariant()) switch
        {
            ("fullname",   "desc") => filtered.OrderByDescending(i => i.FullName),
            ("studentcode","asc")  => filtered.OrderBy(i => i.StudentCode),
            ("studentcode","desc") => filtered.OrderByDescending(i => i.StudentCode),
            ("enterprise", "asc")  => filtered.OrderBy(i => i.EnterpriseName),
            ("enterprise", "desc") => filtered.OrderByDescending(i => i.EnterpriseName),
            ("logbook",    "asc")  => filtered.OrderBy(i => i.Logbook?.PercentComplete ?? 0),
            ("logbook",    "desc") => filtered.OrderByDescending(i => i.Logbook?.PercentComplete ?? 0),
            ("violation",  "asc")  => filtered.OrderBy(i => i.ViolationCount),
            ("violation",  "desc") => filtered.OrderByDescending(i => i.ViolationCount),
            _                      => filtered.OrderBy(i => i.FullName)   // default asc fullname
        };

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        // 13. Pagination
        var paged = filteredList
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var result = new GetUniAdminStudentListResponse
        {
            Students = new PaginatedResult<StudentListItemDto>(paged, totalCount, request.PageNumber, request.PageSize),
            Summary = summary,
            ResolvedTermId = resolvedTermId
        };

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetStudentList),
            currentUserId, resolvedTermId, totalCount);

        return Result<GetUniAdminStudentListResponse>.Success(
            result,
            _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentsRetrieved));
    }

    private static InternshipUiStatus DeriveUiStatus(
        PlacementStatus placementStatus,
        bool hasGroup,
        bool hasPendingApp,
        DateTime? groupEndDate)
    {
        if (placementStatus == PlacementStatus.Unplaced)
            return hasPendingApp ? InternshipUiStatus.PendingConfirmation : InternshipUiStatus.Unplaced;

        if (hasGroup && groupEndDate.HasValue && groupEndDate.Value.Date < DateTime.UtcNow.Date)
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
}

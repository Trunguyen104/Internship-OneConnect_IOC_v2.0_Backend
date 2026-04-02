using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Queries.GetUniAssignApplications;

public class GetUniAssignApplicationsHandler
    : IRequestHandler<GetUniAssignApplicationsQuery, Result<PaginatedResult<GetUniAssignApplicationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAssignApplicationsHandler> _logger;

    public GetUniAssignApplicationsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAssignApplicationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetUniAssignApplicationsResponse>>> Handle(
        GetUniAssignApplicationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<PaginatedResult<GetUniAssignApplicationsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<PaginatedResult<GetUniAssignApplicationsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var query = _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                .Include(a => a.Job).ThenInclude(j => j!.InternshipPhase)
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.University)
                .Where(a => a.EnterpriseId == enterpriseUser.EnterpriseId
                         && a.Source == ApplicationSource.UniAssign);

            if (!request.IncludeTerminal)
                query = query.Where(a => a.Status == InternshipApplicationStatus.PendingAssignment);

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<InternshipApplicationStatus>(request.Status, true, out var statusFilter))
                query = query.Where(a => a.Status == statusFilter);

            if (request.UniversityId.HasValue)
                query = query.Where(a => a.UniversityId == request.UniversityId);

            // Intern Phase filter
            if (request.InternshipPhaseId.HasValue)
                query = query.Where(a => a.Job != null && a.Job.InternshipPhaseId == request.InternshipPhaseId);

            if (!string.IsNullOrWhiteSpace(request.MonthYear) &&
                DateOnly.TryParseExact(request.MonthYear + "-01", "yyyy-MM-dd", out var monthDate))
                query = query.Where(a =>
                    a.AppliedAt.Year == monthDate.Year && a.AppliedAt.Month == monthDate.Month);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    (a.Student.User.FullName != null && a.Student.User.FullName.ToLower().Contains(term)) ||
                    (a.Student.User.Email != null && a.Student.User.Email.ToLower().Contains(term)) ||
                    (a.Student.User.UserCode != null && a.Student.User.UserCode.ToLower().Contains(term)));
            }

            // Badge counts (before sorting/paging)
            var badgeCounts = await query
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("appliedat", "asc") => query.OrderBy(a => a.AppliedAt),
                ("name", "asc") => query.OrderBy(a => a.Student.User.FullName),
                ("name", _) => query.OrderByDescending(a => a.Student.User.FullName),
                _ => query.OrderByDescending(a => a.AppliedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var applications = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Check for AC-C01: does each student have an active Self-apply at this enterprise?
            var studentIds = applications.Select(a => a.StudentId).Distinct().ToList();
            var activeSelfApplyMap = await _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentId)
                         && a.EnterpriseId == enterpriseUser.EnterpriseId
                         && a.Source == ApplicationSource.SelfApply
                         && (a.Status == InternshipApplicationStatus.Applied ||
                             a.Status == InternshipApplicationStatus.Interviewing ||
                             a.Status == InternshipApplicationStatus.Offered))
                .Select(a => new { a.StudentId, a.Status })
                .ToListAsync(cancellationToken);

            var items = applications.Select(a =>
            {
                var selfApply = activeSelfApplyMap.FirstOrDefault(x => x.StudentId == a.StudentId);
                return new GetUniAssignApplicationsResponse
                {
                    ApplicationId = a.ApplicationId,
                    StudentId = a.StudentId,
                    StudentFullName = a.Student?.User?.FullName ?? string.Empty,
                    StudentCode = a.Student?.User?.UserCode ?? string.Empty,
                    StudentEmail = a.Student?.User?.Email ?? string.Empty,
                    UniversityName = a.University?.Name ?? string.Empty,
                    InternshipPhaseId = a.Job?.InternshipPhaseId,
                    InternPhaseName = a.Job?.InternshipPhase?.Name,
                    InternPhaseStartDate = a.Job?.InternshipPhase?.StartDate,
                    InternPhaseEndDate = a.Job?.InternshipPhase?.EndDate,
                    AppliedAt = a.AppliedAt,
                    Status = a.Status,
                    StatusLabel = a.Status.ToString(),
                    HasActiveSelfApply = selfApply != null,
                    ActiveSelfApplyStatus = selfApply?.Status.ToString()
                };
            }).ToList();

            var result = PaginatedResult<GetUniAssignApplicationsResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);

            result.BadgeCounts = badgeCounts.ToDictionary(
                x => x.Status.ToString(),
                x => x.Count);

            return Result<PaginatedResult<GetUniAssignApplicationsResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching uni-assign applications for enterprise user {UserId}", _currentUserService.UserId);
            return Result<PaginatedResult<GetUniAssignApplicationsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}

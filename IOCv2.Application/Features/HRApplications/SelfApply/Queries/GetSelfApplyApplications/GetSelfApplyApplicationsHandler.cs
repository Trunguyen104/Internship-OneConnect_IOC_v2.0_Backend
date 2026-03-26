using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Queries.GetSelfApplyApplications;

public class GetSelfApplyApplicationsHandler
    : IRequestHandler<GetSelfApplyApplicationsQuery, Result<PaginatedResult<GetSelfApplyApplicationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetSelfApplyApplicationsHandler> _logger;

    public GetSelfApplyApplicationsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetSelfApplyApplicationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetSelfApplyApplicationsResponse>>> Handle(
        GetSelfApplyApplicationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<PaginatedResult<GetSelfApplyApplicationsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<PaginatedResult<GetSelfApplyApplicationsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            // Base query: only SelfApply applications for this enterprise
            var query = _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                .Include(a => a.Job)
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Student).ThenInclude(s => s.StudentTerms).ThenInclude(st => st.Term).ThenInclude(t => t.University)
                .Where(a => a.EnterpriseId == enterpriseUser.EnterpriseId
                         && a.Source == ApplicationSource.SelfApply);

            // IncludeTerminal toggle: by default only show active stages
            if (!request.IncludeTerminal)
            {
                query = query.Where(a =>
                    a.Status == InternshipApplicationStatus.Applied ||
                    a.Status == InternshipApplicationStatus.Interviewing ||
                    a.Status == InternshipApplicationStatus.Offered);
            }

            // Status filter (explicit)
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<InternshipApplicationStatus>(request.Status, true, out var statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (request.UniversityId.HasValue)
            {
                query = query.Where(a =>
                    a.Student.StudentTerms.Any(st => st.Term.UniversityId == request.UniversityId));
            }

            // Month-Year filter
            if (!string.IsNullOrWhiteSpace(request.MonthYear) &&
                DateOnly.TryParseExact(request.MonthYear + "-01", "yyyy-MM-dd", out var monthDate))
            {
                query = query.Where(a =>
                    a.AppliedAt.Year == monthDate.Year && a.AppliedAt.Month == monthDate.Month);
            }

            // Job Id / Job Title filter
            if (request.JobId.HasValue)
            {
                query = query.Where(a => a.JobId == request.JobId);
            }
            else if (!string.IsNullOrWhiteSpace(request.JobTitle))
            {
                var jobTitle = request.JobTitle.Trim().ToLower();
                query = query.Where(a => a.Job != null && a.Job.Title.ToLower().Contains(jobTitle));
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    (a.Student.User.FullName != null && a.Student.User.FullName.ToLower().Contains(term)) ||
                    (a.Student.User.Email != null && a.Student.User.Email.ToLower().Contains(term)) ||
                    (a.Student.User.UserCode != null && a.Student.User.UserCode.ToLower().Contains(term)));
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("appliedat", "asc") => query.OrderBy(a => a.AppliedAt),
                ("appliedat", _) => query.OrderByDescending(a => a.AppliedAt),
                ("name", "asc") => query.OrderBy(a => a.Student.User.FullName),
                ("name", _) => query.OrderByDescending(a => a.Student.User.FullName),
                ("status", "asc") => query.OrderBy(a => a.Status),
                ("status", _) => query.OrderByDescending(a => a.Status),
                _ => query.OrderByDescending(a => a.AppliedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var applications = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = applications.Select(a =>
            {
                var latestTerm = a.Student?.StudentTerms?.OrderByDescending(st => st.CreatedAt).FirstOrDefault();
                return new GetSelfApplyApplicationsResponse
                {
                    ApplicationId = a.ApplicationId,
                    StudentId = a.StudentId,
                    StudentFullName = a.Student?.User?.FullName ?? string.Empty,
                    StudentCode = a.Student?.User?.UserCode ?? string.Empty,
                    StudentEmail = a.Student?.User?.Email ?? string.Empty,
                    StudentPhone = a.Student?.User?.PhoneNumber ?? string.Empty,
                    UniversityName = latestTerm?.Term?.University?.Name ?? string.Empty,
                    JobPostingTitle = a.Job?.Title ?? string.Empty,
                    AppliedAt = a.AppliedAt,
                    Status = a.Status,
                    StatusLabel = a.Status.ToString()
                };
            }).ToList();

            var result = PaginatedResult<GetSelfApplyApplicationsResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);

            return Result<PaginatedResult<GetSelfApplyApplicationsResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching self-apply applications for enterprise user {UserId}", _currentUserService.UserId);
            return Result<PaginatedResult<GetSelfApplyApplicationsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}

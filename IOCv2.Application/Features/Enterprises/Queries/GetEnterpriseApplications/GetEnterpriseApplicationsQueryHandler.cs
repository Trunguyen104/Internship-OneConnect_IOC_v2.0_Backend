using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseApplications;

public class GetEnterpriseApplicationsQueryHandler
    : IRequestHandler<GetEnterpriseApplicationsQuery, Result<PaginatedResult<GetEnterpriseApplicationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetEnterpriseApplicationsQueryHandler> _logger;

    public GetEnterpriseApplicationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetEnterpriseApplicationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetEnterpriseApplicationsResponse>>> Handle(
        GetEnterpriseApplicationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
                return Result<PaginatedResult<GetEnterpriseApplicationsResponse>>.Failure("User ID không hợp lệ.", ResultErrorType.Unauthorized);

            var currentUserRole = _currentUserService.Role;

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<PaginatedResult<GetEnterpriseApplicationsResponse>>.Failure("Không tìm thấy thông tin nhân viên doanh nghiệp.", ResultErrorType.Forbidden);

            var enterpriseId = enterpriseUser.EnterpriseId;

            // Base query - filter by enterprise and term
            var query = _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Where(a => a.EnterpriseId == enterpriseId && a.TermId == request.TermId);

            // RBAC: Mentor only sees students in their groups
            if (currentUserRole == UserRole.Mentor.ToString())
            {
                var mentorStudentIds = await _unitOfWork.Repository<InternshipGroup>().Query().AsNoTracking()
                    .Where(ig => ig.EnterpriseId == enterpriseId && ig.MentorId == enterpriseUser.EnterpriseUserId)
                    .SelectMany(ig => ig.Members)
                    .Select(m => m.StudentId)
                    .ToListAsync(cancellationToken);

                query = query.Where(a => mentorStudentIds.Contains(a.StudentId));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(a =>
                    (a.Student.User.FullName != null && a.Student.User.FullName.ToLower().Contains(search)) ||
                    (a.Student.User.Email != null && a.Student.User.Email.ToLower().Contains(search)) ||
                    (a.Student.User.UserCode != null && a.Student.User.UserCode.ToLower().Contains(search)));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<InternshipApplicationStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(a => a.Status == statusEnum);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var applications = await query
                .OrderByDescending(a => a.AppliedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Get mentor/project info for each student
            var studentIds = applications.Select(a => a.StudentId).ToList();
            var groupMemberships = await _unitOfWork.Repository<InternshipStudent>().Query().AsNoTracking()
                .Include(ms => ms.InternshipGroup).ThenInclude(ig => ig.Mentor).ThenInclude(m => m.User)
                .Include(ms => ms.InternshipGroup).ThenInclude(ig => ig.Projects)
                .Where(ms => studentIds.Contains(ms.StudentId) &&
                             ms.InternshipGroup.EnterpriseId == enterpriseId)
                .ToListAsync(cancellationToken);

            var responses = applications.Select(app =>
            {
                var membership = groupMemberships.FirstOrDefault(m => m.StudentId == app.StudentId);
                var mentorName = membership?.InternshipGroup?.Mentor?.User?.FullName;
                var projectName = membership?.InternshipGroup?.Projects?.FirstOrDefault()?.ProjectName;

                return new GetEnterpriseApplicationsResponse
                {
                    ApplicationId = app.ApplicationId,
                    EnterpriseId = app.EnterpriseId,
                    TermId = app.TermId,
                    StudentId = app.StudentId,
                    StudentFullName = app.Student?.User?.FullName ?? "N/A",
                    StudentCode = app.Student?.User?.UserCode ?? "N/A",
                    UniversityName = "Unknown",
                    Major = app.Student?.Major ?? string.Empty,
                    Status = app.Status,
                    RejectReason = app.RejectReason,
                    MentorName = mentorName,
                    ProjectName = projectName,
                    AppliedAt = app.AppliedAt
                };
            }).ToList();

            var paginatedResult = PaginatedResult<GetEnterpriseApplicationsResponse>.Create(
                responses, totalCount, request.PageIndex, request.PageSize);

            return Result<PaginatedResult<GetEnterpriseApplicationsResponse>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching enterprise applications");
            return Result<PaginatedResult<GetEnterpriseApplicationsResponse>>.Failure("Đã xảy ra lỗi hệ thống.", ResultErrorType.InternalServerError);
        }
    }
}

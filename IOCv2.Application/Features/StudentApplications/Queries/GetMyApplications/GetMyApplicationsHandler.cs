using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplications;

public class GetMyApplicationsHandler
    : IRequestHandler<GetMyApplicationsQuery, Result<PaginatedResult<GetMyApplicationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;

    public GetMyApplicationsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
    }

    public async Task<Result<PaginatedResult<GetMyApplicationsResponse>>> Handle(
        GetMyApplicationsQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<PaginatedResult<GetMyApplicationsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

        // Resolve Student from UserId
        var student = await _unitOfWork.Repository<Student>().Query().AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

        if (student == null)
            return Result<PaginatedResult<GetMyApplicationsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.StudentMessageKey.StudentNotFound), ResultErrorType.NotFound);

        // Base query: own applications, not hidden
        var query = _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
            .Include(a => a.Enterprise)
            .Include(a => a.Job)
            .Where(a => a.StudentId == student.StudentId && !a.IsHiddenByStudent);

        // Default: always show active + Placed. IncludeTerminal toggles visibility of Rejected/Withdrawn.
        // Placed (✅) luôn hiển thị theo AC-01 — đây là milestone quan trọng nhất.
        if (!request.IncludeTerminal)
        {
            query = query.Where(a =>
                a.Status == InternshipApplicationStatus.Applied ||
                a.Status == InternshipApplicationStatus.Interviewing ||
                a.Status == InternshipApplicationStatus.Offered ||
                a.Status == InternshipApplicationStatus.PendingAssignment ||
                a.Status == InternshipApplicationStatus.Placed);
        }

        // Filter by explicit status string
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<InternshipApplicationStatus>(request.Status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        // Search by enterprise or job title
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Enterprise.Name.ToLower().Contains(term) ||
                (a.Job != null && a.Job.Title.ToLower().Contains(term)));
        }

        query = query.OrderByDescending(a => a.AppliedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var responses = items.Select(a => new GetMyApplicationsResponse
        {
            ApplicationId = a.ApplicationId,
            Source = a.Source,
            JobTitle = a.Job?.Title,
            IsJobClosed = a.Job != null ? a.Job.Status == JobStatus.CLOSED : null,
            IsJobDeleted = a.Job != null ? a.Job.DeletedAt != null : null,
            EnterpriseName = a.Enterprise.Name,
            EnterpriseLogoUrl = a.Enterprise.LogoUrl,
            Status = a.Status,
            AppliedAt = a.AppliedAt,
            CanWithdraw = a.Source == ApplicationSource.SelfApply && a.Status == InternshipApplicationStatus.Applied,
            CanHide = a.Status is InternshipApplicationStatus.Rejected or InternshipApplicationStatus.Withdrawn
        }).ToList();

        var result = PaginatedResult<GetMyApplicationsResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedResult<GetMyApplicationsResponse>>.Success(result);
    }
}

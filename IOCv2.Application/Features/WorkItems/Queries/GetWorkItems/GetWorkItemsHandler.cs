using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItems;

public class GetWorkItemsHandler : IRequestHandler<GetWorkItemsQuery, Result<PaginatedResult<GetWorkItemsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWorkItemsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaginatedResult<GetWorkItemsResponse>>> Handle(GetWorkItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<WorkItem>().Query()
            .AsNoTracking()
            .Include(w => w.Assignee)
                .ThenInclude(a => a!.User)
            .Where(w => w.ProjectId == request.ProjectId && w.Type != WorkItemType.Epic);

        // Apply filters
        if (request.Status.HasValue)
            query = query.Where(w => w.Status == request.Status.Value);

        if (request.Type.HasValue)
            query = query.Where(w => w.Type == request.Type.Value);

        if (request.Priority.HasValue)
            query = query.Where(w => w.Priority == request.Priority.Value);

        if (request.AssigneeId.HasValue)
            query = query.Where(w => w.AssigneeId == request.AssigneeId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(w => w.Title.ToLower().Contains(term) || 
                                    (w.Assignee != null && w.Assignee.User != null && w.Assignee.User.FullName != null && w.Assignee.User.FullName.ToLower().Contains(term)));
        }

        // Apply sorting
        query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
        {
            ("title", "asc") => query.OrderBy(w => w.Title),
            ("title", "desc") => query.OrderByDescending(w => w.Title),
            ("createdat", "asc") => query.OrderBy(w => w.CreatedAt),
            ("createdat", "desc") => query.OrderByDescending(w => w.CreatedAt),
            ("priority", "asc") => query.OrderBy(w => w.Priority),
            ("priority", "desc") => query.OrderByDescending(w => w.Priority),
            ("status", "asc") => query.OrderBy(w => w.Status),
            ("status", "desc") => query.OrderByDescending(w => w.Status),
            ("duedate", "asc") => query.OrderBy(w => w.DueDate),
            ("duedate", "desc") => query.OrderByDescending(w => w.DueDate),
            _ => query.OrderByDescending(w => w.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => new GetWorkItemsResponse
            {
                WorkItemId = w.WorkItemId,
                Title = w.Title,
                Type = w.Type,
                Status = w.Status,
                Priority = w.Priority,
                StoryPoint = w.StoryPoint,
                AssigneeId = w.AssigneeId,
                AssigneeName = w.Assignee != null && w.Assignee.User != null ? w.Assignee.User.FullName : null,
                AssigneeAvatarUrl = w.Assignee != null && w.Assignee.User != null ? w.Assignee.User.AvatarUrl : null,
                DueDate = w.DueDate,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<GetWorkItemsResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedResult<GetWorkItemsResponse>>.Success(result);
    }
}

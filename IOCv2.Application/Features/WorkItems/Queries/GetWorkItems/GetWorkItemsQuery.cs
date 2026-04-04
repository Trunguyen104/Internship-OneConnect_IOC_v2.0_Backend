using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItems;

public record GetWorkItemsQuery : IRequest<Result<PaginatedResult<GetWorkItemsResponse>>>
{
    public Guid ProjectId { get; init; }
    
    // Pagination parameters
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    
    // Filtering parameters
    public WorkItemStatus? Status { get; init; }
    public WorkItemType? Type { get; init; }
    public Priority? Priority { get; init; }
    public Guid? AssigneeId { get; init; }
    
    // Search parameter (matches title or user code)
    public string? SearchTerm { get; init; }
    
    // Sorting (Column + Order)
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
}

using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Queries.GetBacklog;

public record GetBacklogQuery : IRequest<Result<GetBacklogResponse>>
{
    public Guid ProjectId { get; init; }

    // Filters
    public Guid? EpicId { get; init; }
    public string? SearchTerm { get; init; }

    public WorkItemType? Type { get; init; }

    public Priority? Priority { get; init; }

    public WorkItemStatus? Status { get; init; }

    public Guid? AssigneeId { get; init; }

    /// <summary>
    /// Khi true, chỉ trả về Product Backlog (items chưa trong Sprint nào).
    /// Dùng khi muốn chọn workitems để thêm vào Sprint mới.
    /// </summary>
    public bool BacklogOnly { get; init; } = false;
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Queries.GetBacklog;

public record GetBacklogQuery : IRequest<Result<GetBacklogResponse>>
{
    public Guid ProjectId { get; init; }

    // Filters
    public Guid? EpicId { get; init; }
    public string? SearchTerm { get; init; }

    /// <summary>Epic | UserStory | Task | Subtask</summary>
    public string? Type { get; init; }

    /// <summary>Low | Medium | High | Critical</summary>
    public string? Priority { get; init; }

    /// <summary>Todo | InProgress | Review | Done | Cancelled</summary>
    public string? Status { get; init; }

    public Guid? AssigneeId { get; init; }
}

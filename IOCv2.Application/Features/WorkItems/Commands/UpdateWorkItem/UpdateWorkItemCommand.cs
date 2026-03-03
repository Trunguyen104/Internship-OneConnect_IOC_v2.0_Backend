using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

public record UpdateWorkItemCommand : IRequest<Result<UpdateWorkItemResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid WorkItemId { get; init; }

    public string? Title { get; init; }
    public string? Description { get; init; }

    /// <summary>Low | Medium | High | Critical | null (không đổi)</summary>
    public string? Priority { get; init; }

    /// <summary>Todo | InProgress | Review | Done | Cancelled | null (không đổi)</summary>
    public string? Status { get; init; }

    public int? StoryPoint { get; init; }
    public Guid? AssigneeId { get; init; }
    public DateOnly? DueDate { get; init; }
}

using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;

public record CreateWorkItemCommand : IRequest<Result<CreateWorkItemResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    public string Title { get; init; } = string.Empty;

    /// <summary>Epic | UserStory | Task | Subtask</summary>
    public string Type { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>Low | Medium | High | Critical</summary>
    public string? Priority { get; init; }

    public int? StoryPoint { get; init; }
    public Guid? AssigneeId { get; init; }
    public Guid? ParentId { get; init; }
    public DateOnly? DueDate { get; init; }

    /// <summary>Nếu có → tạo luôn vào Sprint Backlog</summary>
    public Guid? SprintId { get; init; }
}

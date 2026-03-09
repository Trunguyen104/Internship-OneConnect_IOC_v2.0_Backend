using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;

public record CreateWorkItemCommand : IRequest<Result<CreateWorkItemResponse>>
{
    public Guid ProjectId { get; init; }

    public string Title { get; init; } = string.Empty;

    public WorkItemType Type { get; init; }

    public string? Description { get; init; }

    public Priority? Priority { get; init; }

    public int? StoryPoint { get; init; }
    public Guid? AssigneeId { get; init; }
    public Guid? ParentId { get; init; }
    public DateOnly? DueDate { get; init; }

    /// <summary>Nếu có → tạo luôn vào Sprint Backlog</summary>
    public Guid? SprintId { get; init; }
}

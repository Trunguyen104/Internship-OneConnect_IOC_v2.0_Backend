using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

public record UpdateWorkItemCommand : IRequest<Result<UpdateWorkItemResponse>>
{
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid WorkItemId { get; init; }

    public string? Title { get; init; }
    public string? Description { get; init; }

    public Priority? Priority { get; init; }

    public WorkItemStatus? Status { get; init; }

    public int? StoryPoint { get; init; }
    public Guid? AssigneeId { get; init; }
    public DateOnly? DueDate { get; init; }
}

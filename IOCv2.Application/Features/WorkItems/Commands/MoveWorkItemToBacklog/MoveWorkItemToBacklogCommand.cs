using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToBacklog;

public record MoveWorkItemToBacklogCommand : IRequest<Result<MoveWorkItemToBacklogResponse>>
{
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid WorkItemId { get; init; }

    /// <summary>WorkItemId của item đứng trước vị trí mới trong Product Backlog. Null = đặt lên đầu.</summary>
    public Guid? AfterWorkItemId { get; init; }
}

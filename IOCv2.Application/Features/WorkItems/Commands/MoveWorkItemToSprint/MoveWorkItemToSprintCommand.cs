using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToSprint;

public record MoveWorkItemToSprintCommand : IRequest<Result<MoveWorkItemToSprintResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid WorkItemId { get; init; }

    public Guid TargetSprintId { get; init; }

    /// <summary>WorkItemId của item đứng trước vị trí mới. Null = đặt lên đầu danh sách.</summary>
    public Guid? AfterWorkItemId { get; init; }
}

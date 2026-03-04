using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public record CreateSprintCommand : IRequest<Result<CreateSprintResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Goal { get; init; }

    /// <summary>
    /// Optional: WorkItem IDs (children của một Epic) được chọn để thêm vào Sprint ngay khi tạo.
    /// Có thể chọn một số hoặc toàn bộ. Để trống nếu không muốn thêm workitem.
    /// </summary>
    public List<Guid>? WorkItemIds { get; init; }
}


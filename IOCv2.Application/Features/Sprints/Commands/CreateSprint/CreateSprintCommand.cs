using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

/// <summary>
/// Command to create a new sprint.
/// </summary>
public record CreateSprintCommand : IRequest<Result<CreateSprintResponse>>
{
    /// <summary>
    /// The ID of the project the sprint belongs to.
    /// </summary>
    public Guid ProjectId { get; init; }

    /// <summary>
    /// The name of the sprint.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional goal or description for the sprint.
    /// </summary>
    public string? Goal { get; init; }

    /// <summary>
    /// Optional: WorkItem IDs (children của một Epic) được chọn để thêm vào Sprint ngay khi tạo.
    /// Có thể chọn một số hoặc toàn bộ. Để trống nếu không muốn thêm workitem.
    /// </summary>
    public List<Guid>? WorkItemIds { get; init; }
}


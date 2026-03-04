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
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    /// <summary>
    /// The name of the sprint.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional goal or description for the sprint.
    /// </summary>
    public string? Goal { get; init; }
}

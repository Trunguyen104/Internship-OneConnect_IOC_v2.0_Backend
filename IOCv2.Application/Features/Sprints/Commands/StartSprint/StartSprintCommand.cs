using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

/// <summary>
/// Command to start a sprint.
/// </summary>
public record StartSprintCommand : IRequest<Result<StartSprintResponse>>
{
    /// <summary>
    /// The ID of the project the sprint belongs to.
    /// </summary>
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    /// <summary>
    /// The ID of the sprint to start.
    /// </summary>
    [JsonIgnore]
    public Guid SprintId { get; init; }

    /// <summary>
    /// The start date for the sprint.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// The end date for the sprint.
    /// </summary>
    public DateOnly EndDate { get; init; }
}

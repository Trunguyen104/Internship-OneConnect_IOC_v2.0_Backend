using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

/// <summary>
/// Command to update an existing sprint.
/// </summary>
public record UpdateSprintCommand : IRequest<Result<UpdateSprintResponse>>
{
    /// <summary>
    /// The ID of the project the sprint belongs to.
    /// </summary>
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    /// <summary>
    /// The ID of the sprint to update.
    /// </summary>
    [JsonIgnore]
    public Guid SprintId { get; init; }

    /// <summary>
    /// The new name of the sprint.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The new goal of the sprint.
    /// </summary>
    public string? Goal { get; init; }

    /// <summary>
    /// The new start date of the sprint.
    /// </summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>
    /// The new end date of the sprint.
    /// </summary>
    public DateOnly? EndDate { get; init; }
}

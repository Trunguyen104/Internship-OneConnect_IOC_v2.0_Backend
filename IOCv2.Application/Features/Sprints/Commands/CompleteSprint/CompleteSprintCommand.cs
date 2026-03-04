using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

/// <summary>
/// Command to complete an active sprint.
/// </summary>
public record CompleteSprintCommand : IRequest<Result<CompleteSprintResponse>>
{
    /// <summary>
    /// The ID of the project the sprint belongs to.
    /// </summary>
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    /// <summary>
    /// The ID of the sprint to complete.
    /// </summary>
    [JsonIgnore]
    public Guid SprintId { get; init; }
    
    /// <summary>
    /// Option for handling incomplete work items. 
    /// Possible values: "ToBacklog", "ToNextPlannedSprint", "CreateNewSprint".
    /// </summary>
    public string IncompleteItemsOption { get; init; } = string.Empty;

    /// <summary>
    /// Optional target sprint ID if "ToNextPlannedSprint" is chosen.
    /// </summary>
    public Guid? TargetSprintId { get; init; }

    /// <summary>
    /// Optional name for the new sprint if "CreateNewSprint" is chosen.
    /// </summary>
    public string? NewSprintName { get; init; }
}

using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public record UpdateSprintCommand : IRequest<Result<UpdateSprintResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid SprintId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Goal { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}

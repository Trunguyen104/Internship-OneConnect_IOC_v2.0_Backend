using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public record StartSprintCommand : IRequest<Result<StartSprintResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid SprintId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}

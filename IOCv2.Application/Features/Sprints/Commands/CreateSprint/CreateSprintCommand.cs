using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public record CreateSprintCommand : IRequest<Result<CreateSprintResponse>>
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Goal { get; init; }
}

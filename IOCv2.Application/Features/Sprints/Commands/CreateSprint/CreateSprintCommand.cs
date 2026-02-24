using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public record CreateSprintCommand(
    Guid ProjectId,
    string Name,
    string? Goal
) : IRequest<Result<CreateSprintResponse>>;

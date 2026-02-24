using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public record UpdateSprintCommand(
    Guid SprintId,
    string Name,
    string? Goal,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<Result<UpdateSprintResponse>>;

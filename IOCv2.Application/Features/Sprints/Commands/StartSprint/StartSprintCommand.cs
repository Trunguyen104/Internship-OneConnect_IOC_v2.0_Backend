using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public record StartSprintCommand(Guid SprintId) : IRequest<Result<bool>>;

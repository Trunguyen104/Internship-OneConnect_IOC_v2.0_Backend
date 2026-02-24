using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

public record DeleteSprintCommand(Guid SprintId) : IRequest<Result>;

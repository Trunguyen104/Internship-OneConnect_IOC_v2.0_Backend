using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public record CompleteSprintCommand(
    Guid SprintId,
    MoveIncompleteItemsOption IncompleteItemsOption
) : IRequest<Result<CompleteSprintResponse>>;

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprintById;

public record GetSprintByIdQuery(Guid SprintId) : IRequest<Result<GetSprintByIdResponse>>;

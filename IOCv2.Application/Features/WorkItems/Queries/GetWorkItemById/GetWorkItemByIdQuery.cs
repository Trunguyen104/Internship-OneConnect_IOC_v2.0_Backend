using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItemById;

public record GetWorkItemByIdQuery(Guid ProjectId, Guid WorkItemId) : IRequest<Result<GetWorkItemByIdResponse>>;

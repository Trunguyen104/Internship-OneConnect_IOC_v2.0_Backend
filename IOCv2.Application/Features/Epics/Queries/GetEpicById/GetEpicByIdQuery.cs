using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Epics.Queries.GetEpicById;

public record GetEpicByIdQuery(Guid ProjectId, Guid EpicId) : IRequest<Result<GetEpicByIdResponse>>;

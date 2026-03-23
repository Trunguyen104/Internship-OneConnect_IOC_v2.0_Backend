using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Universities.Queries.GetUniversityById;

public record GetUniversityByIdQuery(Guid UniversityId) : IRequest<Result<GetUniversityByIdResponse>>;

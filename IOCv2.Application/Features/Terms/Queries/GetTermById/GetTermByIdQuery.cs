using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Terms.Queries.GetTermById;

public record GetTermByIdQuery(Guid TermId) : IRequest<Result<GetTermByIdResponse>>;
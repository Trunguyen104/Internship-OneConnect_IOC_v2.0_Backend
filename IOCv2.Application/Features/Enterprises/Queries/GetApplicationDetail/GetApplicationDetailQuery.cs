using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Queries.GetApplicationDetail;

public record GetApplicationDetailQuery(Guid ApplicationId) : IRequest<Result<GetApplicationDetailResponse>>;

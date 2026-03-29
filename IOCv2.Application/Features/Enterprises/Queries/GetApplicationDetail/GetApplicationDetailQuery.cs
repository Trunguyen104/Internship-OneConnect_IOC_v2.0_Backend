using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Queries.GetApplicationDetail;

public record GetApplicationDetailQuery : IRequest<Result<GetApplicationDetailResponse>>
{
    public Guid ApplicationId { get; init; }
}

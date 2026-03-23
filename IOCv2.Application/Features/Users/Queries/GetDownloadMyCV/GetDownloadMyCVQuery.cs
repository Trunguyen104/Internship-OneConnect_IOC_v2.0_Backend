using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Users.Queries.GetDownloadMyCV
{
    public record GetDownloadMyCVQuery : IRequest<Result<GetDownloadMyCVResponse>>
    {
    }
}

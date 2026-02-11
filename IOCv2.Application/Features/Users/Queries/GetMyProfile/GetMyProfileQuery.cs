using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Users.Queries.GetMyProfile
{
    public record GetMyProfileQuery(Guid UserId) : IRequest<Result<GetMyProfileResponse>>;
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Users.Queries.GetMyProfile
{
    public record GetMyProfileQuery : IRequest<Result<GetMyProfileResponse>>
    {
        public Guid UserId { get; init; }
    }
}

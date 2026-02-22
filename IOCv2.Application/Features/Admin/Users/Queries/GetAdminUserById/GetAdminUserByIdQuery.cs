using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUserById
{
    public record GetAdminUserByIdQuery : IRequest<Result<GetAdminUserByIdResponse>>
    {
        public Guid UserId { get; init; }
    }
}

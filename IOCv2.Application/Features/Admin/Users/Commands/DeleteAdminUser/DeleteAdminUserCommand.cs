using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser
{
    public record DeleteAdminUserCommand : IRequest<Result<DeleteAdminUserResponse>>
    {
        public Guid UserId { get; init; }
    }
}

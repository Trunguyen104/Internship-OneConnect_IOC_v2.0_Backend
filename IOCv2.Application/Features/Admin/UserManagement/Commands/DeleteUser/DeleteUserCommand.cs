using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser
{
    public record DeleteUserCommand : IRequest<Result<DeleteUserResponse>>
    {
        public Guid UserId { get; init; }
    }
}

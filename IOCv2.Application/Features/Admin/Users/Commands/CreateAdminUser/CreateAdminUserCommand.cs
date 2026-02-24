using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser
{
    public record CreateAdminUserCommand : IRequest<Result<CreateAdminUserResponse>>, IMapFrom<User>
    {
        public string FullName { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string Role { get; init; } = null!;
        public Guid? UnitId { get; init; }
    }
}

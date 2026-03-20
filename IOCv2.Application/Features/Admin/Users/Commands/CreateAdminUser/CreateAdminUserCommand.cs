using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser
{
    /// <summary>
    /// Command to create a new administrative user.
    /// </summary>
    public record CreateAdminUserCommand : IRequest<Result<CreateAdminUserResponse>>, IMapFrom<User>
    {
        /// <summary>
        /// Full name of the user.
        /// </summary>
        public string FullName { get; init; } = null!;

        /// <summary>
        /// Unique email address for login and notifications.
        /// </summary>
        public string Email { get; init; } = null!;

        ///// <summary>
        ///// Initial password for the user.
        ///// </summary>
        //public string Password { get; init; } = null!;

        /// <summary>
        /// Role to be assigned (e.g., SchoolAdmin, EnterpriseAdmin).
        /// </summary>
        public UserRole Role { get; init; }

        /// <summary>
        /// (Optional) Phone number.
        /// </summary>
        public string? PhoneNumber { get; init; }

        /// <summary>
        /// (Optional) ID of the linked University or Enterprise.
        /// </summary>
        public Guid? UnitId { get; init; }
    }
}

using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.UpdateUser
{
    /// <summary>
    /// Command to update an existing user with role-based validation.
    /// </summary>
    public record UpdateUserCommand : IRequest<Result<UpdateUserResponse>>
    {
        /// <summary>
        /// ID of the user to update (from route).
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid UserId { get; init; }

        /// <summary>
        /// New full name.
        /// </summary>
        public string FullName { get; init; } = null!;

        /// <summary>
        /// (Optional) New phone number.
        /// </summary>
        public string? PhoneNumber { get; init; }

        /// <summary>
        /// (Optional) New account status.
        /// </summary>
        public UserStatus? Status { get; init; }

        /// <summary>
        /// (Optional) Date of birth.
        /// </summary>
        public string? DateOfBirth { get; init; }

        /// <summary>
        /// (Optional) Gender.
        /// </summary>
        public UserGender? Gender { get; init; }

        /// <summary>
        /// (Optional) New avatar URL.
        /// </summary>
        public string? AvatarUrl { get; init; }

        /// <summary>
        /// (Optional) Student Class Name if role is Student.
        /// </summary>
        public string? StudentClass { get; init; }

        /// <summary>
        /// (Optional) Student Major if role is Student.
        /// </summary>
        public string? StudentMajor { get; init; }

        /// <summary>
        /// (Optional) Student GPA if role is Student.
        /// </summary>
        public decimal? StudentGpa { get; init; }

        /// <summary>
        /// (Optional) ID of the linked University or Enterprise.
        /// </summary>
        public Guid? UnitId { get; init; }

        /// <summary>
        /// (Optional) Home address.
        /// </summary>
        public string? Address { get; init; }
    }
}

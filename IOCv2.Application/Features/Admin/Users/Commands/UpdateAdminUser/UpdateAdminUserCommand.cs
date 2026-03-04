using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    /// <summary>
    /// Command to update an existing administrative user.
    /// </summary>
    public record UpdateAdminUserCommand : IRequest<Result<UpdateAdminUserResponse>>
    {
        /// <summary>
        /// ID of the user to update (from route).
        /// </summary>
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
        public string? Status { get; init; }

        /// <summary>
        /// (Optional) Date of birth.
        /// </summary>
        public string? DateOfBirth { get; init; }

        /// <summary>
        /// (Optional) Gender.
        /// </summary>
        public string? Gender { get; init; }

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
    }
}

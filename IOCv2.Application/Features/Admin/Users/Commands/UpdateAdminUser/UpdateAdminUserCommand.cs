using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    public record UpdateAdminUserCommand : IRequest<Result<UpdateAdminUserResponse>>
    {
        public Guid UserId { get; init; }
        public string FullName { get; init; } = null!;
        public string? PhoneNumber { get; init; }
        public string? Status { get; init; }
        public string? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? AvatarUrl { get; init; }
        public string? StudentClass { get; init; }
        public string? StudentMajor { get; init; }
        public decimal? StudentGpa { get; init; }
    }
}

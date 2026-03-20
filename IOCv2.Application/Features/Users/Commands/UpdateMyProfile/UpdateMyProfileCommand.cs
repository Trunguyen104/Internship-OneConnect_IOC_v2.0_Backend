using IOCv2.Domain.Enums;
using MediatR;
using IOCv2.Application.Common.Models;

namespace IOCv2.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileCommand : IRequest<Result<Unit>>
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public UserGender? Gender { get; set; }
        
        // Metadata
        public string? PortfolioUrl { get; set; }
        public string? Bio { get; set; }
        public string? Expertise { get; set; }
        public string? Department { get; set; }
    }
}

using IOCv2.Domain.Enums;
using MediatR;
using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace IOCv2.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileCommand : IRequest<Result<Unit>>
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public UserGender? Gender { get; set; }
        public string? Position { get; set; }

        
        // Metadata
        public string? PortfolioUrl { get; set; }
        public string? CvUrl { get; set; }
        public string? Bio { get; set; }
        public string? Expertise { get; set; }
        public string? Department { get; set; }
        public string? Major { get; set; }
        public string? ClassName { get; set; }
        public IFormFile? CvFile { get; set; }
    }
}

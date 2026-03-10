using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    public class UpdateAdminUserResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? UserCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, UpdateAdminUserResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));
        }
    }
}

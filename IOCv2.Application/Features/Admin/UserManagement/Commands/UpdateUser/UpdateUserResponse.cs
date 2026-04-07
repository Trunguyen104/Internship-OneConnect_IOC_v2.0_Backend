using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.UpdateUser
{
    public class UpdateUserResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? UserCode { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public UserGender? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, UpdateUserResponse>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => 
                    Enum.IsDefined(typeof(UserGender), src.Gender) ? (UserGender?)src.Gender : UserGender.Other))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));
        }
    }
}

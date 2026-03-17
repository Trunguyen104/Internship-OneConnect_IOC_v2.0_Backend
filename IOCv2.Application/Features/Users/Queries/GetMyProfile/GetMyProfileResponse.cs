using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Users.Queries.GetMyProfile
{
    public class GetMyProfileResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string UserCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public string? AvatarUrl { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? UniversityId { get; set; }
        public Guid? EnterpriseId { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, GetMyProfileResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentId : (Guid?)null))
                .ForMember(dest => dest.UniversityId, opt => opt.MapFrom(src => src.UniversityUser != null ? src.UniversityUser.UniversityId : (Guid?)null))
                .ForMember(dest => dest.EnterpriseId, opt => opt.MapFrom(src => src.EnterpriseUser != null ? src.EnterpriseUser.EnterpriseId : (Guid?)null));
        }
    }
}

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
        public Guid? UnitId { get; set; }

        public string? UnitName { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? CvUrl { get; set; }
        public string? Major { get; set; }
        public string? ClassName { get; set; }
        public string? Bio { get; set; }
        public string? Expertise { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, GetMyProfileResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentId : (Guid?)null))
                .ForMember(dest => dest.UniversityId, opt => opt.MapFrom(src => src.UniversityUser != null ? src.UniversityUser.UniversityId : (Guid?)null))
                .ForMember(dest => dest.EnterpriseId, opt => opt.MapFrom(src => src.EnterpriseUser != null ? src.EnterpriseUser.EnterpriseId : (Guid?)null))
                .ForMember(dest => dest.UnitId, opt => opt.MapFrom(src => 
                    (src.Role == UserRole.SchoolAdmin || src.Role == UserRole.Student) ? (src.UniversityUser != null ? src.UniversityUser.UniversityId : (Guid?)null) :
                    (src.Role == UserRole.EnterpriseAdmin || src.Role == UserRole.HR || src.Role == UserRole.Mentor) ? (src.EnterpriseUser != null ? src.EnterpriseUser.EnterpriseId : (Guid?)null) :
                    null))
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => 
                    (src.Role == UserRole.SchoolAdmin || src.Role == UserRole.Student) ? (src.UniversityUser != null && src.UniversityUser.University != null ? src.UniversityUser.University.Name : null) :
                    (src.Role == UserRole.EnterpriseAdmin || src.Role == UserRole.HR || src.Role == UserRole.Mentor) ? (src.EnterpriseUser != null && src.EnterpriseUser.Enterprise != null ? src.EnterpriseUser.Enterprise.Name : null) :
                    null))
                .ForMember(dest => dest.PortfolioUrl, opt => opt.MapFrom(src => src.Student != null ? src.Student.PortfolioUrl : null))
                .ForMember(dest => dest.CvUrl, opt => opt.MapFrom(src => src.Student != null ? src.Student.CvUrl : null))
                .ForMember(dest => dest.Major, opt => opt.MapFrom(src => src.Student != null ? src.Student.Major : null))
                .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Student != null ? src.Student.ClassName : null))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => 
                    src.EnterpriseUser != null ? src.EnterpriseUser.Bio : 
                    src.UniversityUser != null ? src.UniversityUser.Bio : null))
                .ForMember(dest => dest.Expertise, opt => opt.MapFrom(src => src.EnterpriseUser != null ? src.EnterpriseUser.Expertise : null))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.UniversityUser != null ? src.UniversityUser.Department : null))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => 
                    src.EnterpriseUser != null ? src.EnterpriseUser.Position : 
                    src.UniversityUser != null ? src.UniversityUser.Position : null));
        }
    }
}

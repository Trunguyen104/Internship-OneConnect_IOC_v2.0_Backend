using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUserById
{
    public class GetUserByIdResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string UnitType { get; set; } = string.Empty; // "Internal", "University", "Enterprise"
        public DateTime CreatedAt { get; set; }
        public string? StudentClass { get; set; }
        public string? StudentMajor { get; set; }
        public decimal? StudentGpa { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, GetUserByIdResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.UnitId, opt => opt.MapFrom(src => 
                    src.UniversityUser != null ? src.UniversityUser.UniversityId : 
                    src.EnterpriseUser != null ? src.EnterpriseUser.EnterpriseId : (Guid?)null))
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => 
                    src.UniversityUser != null ? src.UniversityUser.University.Name : 
                    src.EnterpriseUser != null ? src.EnterpriseUser.Enterprise.Name : null))
                .ForMember(dest => dest.UnitType, opt => opt.MapFrom(src => 
                    src.UniversityUser != null ? "University" : 
                    src.EnterpriseUser != null ? "Enterprise" : "Internal"))
                .ForMember(dest => dest.StudentClass, opt => opt.MapFrom(src => src.Student != null ? src.Student.ClassName : null))
                .ForMember(dest => dest.StudentMajor, opt => opt.MapFrom(src => src.Student != null ? src.Student.Major : null))
                .ForMember(dest => dest.StudentGpa, opt => opt.MapFrom(src => src.Student != null ? src.Student.Gpa : (decimal?)null));
        }
    }
}

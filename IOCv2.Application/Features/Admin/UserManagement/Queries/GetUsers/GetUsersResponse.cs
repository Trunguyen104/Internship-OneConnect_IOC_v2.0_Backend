using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers
{
    public class GetUsersResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string? UserCode { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public string? UnitName { get; set; } // University Name or Enterprise Name
        public Guid? UnitId { get; set; }     // UniversityId or EnterpriseId
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, GetUsersResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => 
                    src.UniversityUser != null ? src.UniversityUser.University.Name : 
                    src.EnterpriseUser != null ? src.EnterpriseUser.Enterprise.Name : null))
                .ForMember(dest => dest.UnitId, opt => opt.MapFrom(src => 
                    src.UniversityUser != null ? src.UniversityUser.UniversityId : 
                    src.EnterpriseUser != null ? src.EnterpriseUser.EnterpriseId : (Guid?)null));
        }
    }
}

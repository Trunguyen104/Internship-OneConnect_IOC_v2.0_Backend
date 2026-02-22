using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser
{
    public class DeleteAdminUserResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<User, DeleteAdminUserResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}

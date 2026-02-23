using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus
{
    public class ToggleUserStatusResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<User, ToggleUserStatusResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}

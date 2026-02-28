using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupResponse : IMapFrom<InternshipGroup>
    {
        public Guid InternshipId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<InternshipGroup, DeleteInternshipGroupResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}

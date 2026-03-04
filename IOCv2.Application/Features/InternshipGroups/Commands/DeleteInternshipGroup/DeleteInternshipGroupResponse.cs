using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    /// <summary>
    /// Response model returned after successfully deleting an internship group.
    /// </summary>
    public class DeleteInternshipGroupResponse : IMapFrom<InternshipGroup>
    {
        /// <summary>
        /// Identity of the deleted internship group.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Name of the deleted group.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Status of the group before deletion.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<InternshipGroup, DeleteInternshipGroupResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}

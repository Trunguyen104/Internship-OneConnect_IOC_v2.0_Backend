using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    /// <summary>
    /// Response model returned after successfully updating an internship group.
    /// </summary>
    public class UpdateInternshipGroupResponse : IMapFrom<InternshipGroup>
    {
        /// <summary>
        /// Unique identifier of the updated internship group.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Updated group name.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Updated term identity.
        /// </summary>
        public Guid TermId { get; set; }

        /// <summary>
        /// Group status after update.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<InternshipGroup, UpdateInternshipGroupResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}

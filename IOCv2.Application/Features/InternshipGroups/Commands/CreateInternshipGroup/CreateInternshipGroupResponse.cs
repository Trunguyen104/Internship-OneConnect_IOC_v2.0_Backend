using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;


namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    /// <summary>
    /// Response model returned after successfully creating an internship group.
    /// </summary>
    public class CreateInternshipGroupResponse : IMapFrom<InternshipGroup>
    {
        /// <summary>
        /// Unique identifier for the newly created internship group.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Name of the created group.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// The internship phase associated with this group.
        /// </summary>
        public Guid PhaseId { get; set; }

        /// <summary>
        /// Current lifecycle status of the group.
        /// </summary>
        public InternshipStatus Status { get; set; }


        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<InternshipGroup, CreateInternshipGroupResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        }
    }
}

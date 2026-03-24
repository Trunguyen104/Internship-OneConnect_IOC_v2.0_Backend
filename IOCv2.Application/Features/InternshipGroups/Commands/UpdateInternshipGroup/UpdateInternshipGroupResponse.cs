using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;


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
        /// Updated phase identity.
        /// </summary>
        public Guid PhaseId { get; set; }

        /// <summary>
        /// Group status after update.
        /// </summary>
        public InternshipStatus Status { get; set; }


        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<InternshipGroup, UpdateInternshipGroupResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        }
    }
}

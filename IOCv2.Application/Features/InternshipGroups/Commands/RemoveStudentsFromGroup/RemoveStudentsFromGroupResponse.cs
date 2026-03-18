using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    /// <summary>
    /// Response model returned after removing students from a group.
    /// </summary>
    public class RemoveStudentsFromGroupResponse : IMapFrom<InternshipGroup>
    {
        /// <summary>
        /// Identity of the internship group.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Name of the group.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
    }
}

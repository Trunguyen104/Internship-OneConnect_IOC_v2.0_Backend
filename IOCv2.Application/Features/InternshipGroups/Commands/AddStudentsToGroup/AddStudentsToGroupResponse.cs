using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    /// <summary>
    /// Response model returned after adding students to a group.
    /// </summary>
    public class AddStudentsToGroupResponse : IMapFrom<InternshipGroup>
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

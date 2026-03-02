using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupResponse : IMapFrom<InternshipGroup>
    {
        public Guid InternshipId { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }
}

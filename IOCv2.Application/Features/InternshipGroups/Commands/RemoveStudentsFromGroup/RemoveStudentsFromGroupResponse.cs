using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public class RemoveStudentsFromGroupResponse : IMapFrom<InternshipGroup>
    {
        public Guid InternshipId { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }
}

using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue
{
    public class DeleteStakeholderIssueResponse : IMapFrom<StakeholderIssue>
    {
        public Guid Id { get; set; }

        public DateTime DeletedAt { get; set; }
    }
}


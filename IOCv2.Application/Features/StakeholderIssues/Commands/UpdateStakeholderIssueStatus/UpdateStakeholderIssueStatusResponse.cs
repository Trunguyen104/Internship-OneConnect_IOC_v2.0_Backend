using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;


namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    public class UpdateStakeholderIssueStatusResponse : IMapFrom<StakeholderIssue>
    {
        public Guid Id { get; set; }
        public StakeholderIssueStatus Status { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public void Mapping(AutoMapper.Profile profile)
        {
            profile.CreateMap<StakeholderIssue, UpdateStakeholderIssueStatusResponse>();

        }
    }
}


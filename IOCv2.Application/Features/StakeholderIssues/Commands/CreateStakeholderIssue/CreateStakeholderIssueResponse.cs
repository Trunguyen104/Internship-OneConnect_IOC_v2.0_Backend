using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;


namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue
{
    public class CreateStakeholderIssueResponse : IMapFrom<StakeholderIssue>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public StakeholderIssueStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public void Mapping(AutoMapper.Profile profile)
        {
            profile.CreateMap<StakeholderIssue, CreateStakeholderIssueResponse>();

        }
    }
}


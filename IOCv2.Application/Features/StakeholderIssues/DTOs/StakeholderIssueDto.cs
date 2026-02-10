using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StakeholderIssues.DTOs;

public class StakeholderIssueDto : IMapFrom<StakeholderIssue>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StakeholderIssueStatus Status { get; set; }
    public Guid StakeholderId { get; set; }
    public string StakeholderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public void Mapping(AutoMapper.Profile profile)
    {
        profile.CreateMap<StakeholderIssue, StakeholderIssueDto>()
            .ForMember(d => d.StakeholderName, opt => opt.MapFrom(s => s.Stakeholder.Name));
    }
}

using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public class UpdateEpicResponse : IMapFrom<WorkItem>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public void Mapping(Profile profile)
    {
        profile.CreateMap<WorkItem, UpdateEpicResponse>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.WorkItemId));
    }
}

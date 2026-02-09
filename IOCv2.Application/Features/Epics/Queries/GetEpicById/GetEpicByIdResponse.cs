using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Epics.Queries.GetEpicById;

public class GetEpicByIdResponse : IMapFrom<WorkItem>
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public void Mapping(Profile profile)
    {
        profile.CreateMap<WorkItem, GetEpicByIdResponse>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.WorkItemId))
            .ForMember(d => d.ChildrenCount, opt => opt.MapFrom(s => s.Children.Count));
    }
}

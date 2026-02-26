using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using AutoMapper;

namespace IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;

public class CreateWorkItemResponse : IMapFrom<WorkItem>
{
    public Guid WorkItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? StoryPoint { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateOnly? DueDate { get; set; }
    public float BacklogOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WorkItem, CreateWorkItemResponse>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status != null ? s.Status.ToString() : null))
            .ForMember(d => d.Priority, opt => opt.MapFrom(s => s.Priority != null ? s.Priority.ToString() : null));
    }
}

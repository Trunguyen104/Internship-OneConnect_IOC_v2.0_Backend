using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using AutoMapper;


namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItemById;

public class GetWorkItemByIdResponse : IMapFrom<WorkItem>
{
    public Guid WorkItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemType Type { get; set; }
    public WorkItemStatus? Status { get; set; }
    public Priority? Priority { get; set; }

    public int? StoryPoint { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateOnly? DueDate { get; set; }
    public float BacklogOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WorkItem, GetWorkItemByIdResponse>()
            .ForMember(d => d.AssigneeName, opt => opt.MapFrom(s =>

                s.Assignee != null && s.Assignee.User != null ? s.Assignee.User.FullName : null));
    }
}

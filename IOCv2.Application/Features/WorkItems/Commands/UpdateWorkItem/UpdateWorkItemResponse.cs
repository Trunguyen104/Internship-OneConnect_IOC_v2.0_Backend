using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using AutoMapper;


namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

public class UpdateWorkItemResponse : IMapFrom<WorkItem>
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemType Type { get; set; }
    public WorkItemStatus? Status { get; set; }
    public Priority? Priority { get; set; }

    public int? StoryPoint { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WorkItem, UpdateWorkItemResponse>();

    }
}

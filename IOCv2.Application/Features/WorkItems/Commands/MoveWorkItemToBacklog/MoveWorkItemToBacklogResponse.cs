namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToBacklog;

public class MoveWorkItemToBacklogResponse
{
    public Guid WorkItemId { get; set; }
    public float BacklogOrder { get; set; }
}

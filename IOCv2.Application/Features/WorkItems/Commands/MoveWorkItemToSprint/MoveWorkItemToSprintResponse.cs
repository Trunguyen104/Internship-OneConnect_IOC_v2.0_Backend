namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToSprint;

public class MoveWorkItemToSprintResponse
{
    public Guid WorkItemId { get; set; }
    public Guid SprintId { get; set; }
    public float BoardOrder { get; set; }
}

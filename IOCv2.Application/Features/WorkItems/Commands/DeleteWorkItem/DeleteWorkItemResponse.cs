namespace IOCv2.Application.Features.WorkItems.Commands.DeleteWorkItem;

public class DeleteWorkItemResponse
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
}

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintResponse
{
    public Guid SprintId { get; set; }
    public int CompletedItemsCount { get; set; }
    public int IncompleteItemsCount { get; set; }
    public int MovedItemsCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

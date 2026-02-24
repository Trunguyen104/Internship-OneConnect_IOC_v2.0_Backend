namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

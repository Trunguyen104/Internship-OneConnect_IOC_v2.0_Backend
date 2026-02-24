namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
}

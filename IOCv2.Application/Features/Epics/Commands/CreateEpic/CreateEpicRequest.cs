namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public class CreateEpicRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

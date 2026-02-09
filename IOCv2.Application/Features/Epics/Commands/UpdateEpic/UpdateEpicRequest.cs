namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public class UpdateEpicRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

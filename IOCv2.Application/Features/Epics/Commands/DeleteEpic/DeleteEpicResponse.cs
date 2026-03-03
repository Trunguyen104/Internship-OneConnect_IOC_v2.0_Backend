namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicResponse
{
    public Guid Id { get; set; }
    public bool Success { get; set; } = true;
}

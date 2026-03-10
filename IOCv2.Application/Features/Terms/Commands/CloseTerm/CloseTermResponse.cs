namespace IOCv2.Application.Features.Terms.Commands.CloseTerm;

public class CloseTermResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Warning { get; set; }
    public int UnplacedStudentsCount { get; set; }
}
namespace IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;

public class AddStudentManualResponse
{
    public Guid StudentTermId { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
}

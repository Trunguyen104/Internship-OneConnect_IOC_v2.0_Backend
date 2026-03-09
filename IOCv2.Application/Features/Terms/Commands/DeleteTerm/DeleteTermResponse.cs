namespace IOCv2.Application.Features.Terms.Commands.DeleteTerm;

public class DeleteTermResponse
{
    public string Message { get; set; } = string.Empty;
    public bool HasRelatedData { get; set; }
    public int RelatedStudentTermsCount { get; set; }
    public int RelatedInternshipGroupsCount { get; set; }
}
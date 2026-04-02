using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAdminInternship.Common;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentList;

public class GetUniAdminStudentListResponse
{
    public PaginatedResult<StudentListItemDto> Students { get; set; } = null!;
    public SummaryCardsDto Summary { get; set; } = null!;
    public Guid ResolvedTermId { get; set; }
}

public class SummaryCardsDto
{
    public int TotalStudents { get; set; }
    public int Placed { get; set; }
    public int Unplaced { get; set; }
    public int NoMentor { get; set; }
}

public class StudentListItemDto
{
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public Guid? EnterpriseId { get; set; }
    public string? EnterpriseName { get; set; }
    public string? MentorName { get; set; }
    public LogbookSummaryDto? Logbook { get; set; }
    public InternshipUiStatus InternshipStatus { get; set; }
    public string? ApplicationSource { get; set; }
    public int ViolationCount { get; set; }
}

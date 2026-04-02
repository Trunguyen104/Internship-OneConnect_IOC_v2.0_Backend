using IOCv2.Application.Features.UniAdminInternship.Common;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookTotal;

public class GetUniAdminStudentLogbookTotalResponse
{
    public Guid StudentId { get; set; }
    public Guid ResolvedTermId { get; set; }

    // Thông tin nhóm thực tập
    public Guid? InternshipGroupId { get; set; }
    public string? InternshipGroupName { get; set; }
    public string? EnterpriseName { get; set; }
    public string? MentorName { get; set; }
    public DateTime? GroupStartDate { get; set; }
    public DateTime? GroupEndDate { get; set; }

    // Thông tin sinh viên trong nhóm
    public string? InternshipRole { get; set; }
    public DateTime? JoinedAt { get; set; }

    public LogbookSummaryDto? Logbook { get; set; }
}


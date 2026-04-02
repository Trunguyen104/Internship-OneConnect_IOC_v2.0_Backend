namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentViolations;

public class GetUniAdminStudentViolationsResponse
{
    public int TotalViolations { get; set; }
    public List<ViolationItemDto> Violations { get; set; } = new();
}

public class ViolationItemDto
{
    public Guid ViolationReportId { get; set; }
    public DateOnly OccurredDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid InternshipGroupId { get; set; }
    public string InternshipGroupName { get; set; } = string.Empty;
    public string? EnterpriseName { get; set; }
    public string? ReporterName { get; set; }
}

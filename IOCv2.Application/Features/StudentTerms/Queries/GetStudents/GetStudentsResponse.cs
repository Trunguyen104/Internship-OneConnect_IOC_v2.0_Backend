using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudents;

public class GetStudentsResponse
{
    public Guid StudentTermId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Major { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public PlacementStatus PlacementStatus { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public string? EnrollmentNote { get; set; }
    public Guid? EnterpriseId { get; set; }
    public string? EnterpriseName { get; set; }
}

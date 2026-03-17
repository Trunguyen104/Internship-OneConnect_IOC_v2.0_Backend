using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudents;

public record GetStudentsResponse
{
    public Guid StudentTermId { get; init; }
    public Guid StudentId { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Major { get; init; }
    public string? AvatarUrl { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public EnrollmentStatus EnrollmentStatus { get; init; }
    public PlacementStatus PlacementStatus { get; init; }
    public Guid? EnterpriseId { get; init; }
    public string? EnterpriseName { get; init; }
    public DateOnly EnrollmentDate { get; init; }
}

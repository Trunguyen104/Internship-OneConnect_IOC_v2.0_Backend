using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Users.Queries.GetMyFullContext;

public class GetMyFullContextResponse
{
    public StudentContextInfo StudentInfo { get; set; } = new();
    public UniversityContextInfo? University { get; set; }
    public TermContextInfo? CurrentTerm { get; set; }
    public InternshipContextInfo? Internship { get; set; }
}

public class StudentContextInfo
{
    public Guid StudentId { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public decimal? Gpa { get; set; }
}

public class UniversityContextInfo
{
    public Guid UniversityId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TermContextInfo
{
    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TermDisplayStatus Status { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
}

public class InternshipContextInfo
{
    public InternshipPhaseContextInfo? Phase { get; set; }
    public EnterpriseContextInfo? Enterprise { get; set; }
    public MentorContextInfo? Mentor { get; set; }
    public GroupContextInfo? Group { get; set; }
    public ProjectContextInfo? Project { get; set; }
}

public class InternshipPhaseContextInfo
{
    public Guid PhaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public InternshipPhaseStatus Status { get; set; }
}

public class EnterpriseContextInfo
{
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MentorContextInfo
{
    public Guid MentorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class GroupContextInfo
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
}

public class ProjectContextInfo
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public OperationalStatus Status { get; set; }
}

using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhaseById;

public class GetInternshipPhaseByIdResponse
{
    public Guid PhaseId { get; set; }
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string MajorFields { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RemainingCapacity { get; set; }
    public string? Description { get; set; }
    public InternshipPhaseLifecycleStatus Status { get; set; }
    public int GroupCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Tab: Job Postings ──────────────────────────────────────────────────────
    /// <summary>Danh sách job posting đang gắn vào intern phase này.</summary>
    public IReadOnlyList<PhaseJobPostingDto> JobPostings { get; set; } = Array.Empty<PhaseJobPostingDto>();

    // ── Tab: Sinh viên (Placed) ────────────────────────────────────────────────
    /// <summary>Danh sách SV đang Placed trong phase này.</summary>
    public IReadOnlyList<PhasePlacedStudentDto> PlacedStudents { get; set; } = Array.Empty<PhasePlacedStudentDto>();
}

/// <summary>Job posting summary shown inside the Intern Phase detail page.</summary>
public class PhaseJobPostingDto
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public JobStatus? Status { get; set; }
    /// <summary>ExpireDate displayed as Deadline on the UI.</summary>
    public DateTime? Deadline { get; set; }
    /// <summary>Total number of applications (all statuses) for this job.</summary>
    public int ApplicationCount { get; set; }
}

/// <summary>Student row shown in the "Sinh viên" tab of the Intern Phase detail.</summary>
public class PhasePlacedStudentDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    /// <summary>Tên trường của sinh viên (từ Term → University).</summary>
    public string UniversityName { get; set; } = string.Empty;
    /// <summary>Nguồn: SelfApply / UniAssign.</summary>
    public ApplicationSource Source { get; set; }
    /// <summary>Ngày application được chuyển sang Placed (ReviewedAt).</summary>
    public DateTime? PlacedAt { get; set; }
}

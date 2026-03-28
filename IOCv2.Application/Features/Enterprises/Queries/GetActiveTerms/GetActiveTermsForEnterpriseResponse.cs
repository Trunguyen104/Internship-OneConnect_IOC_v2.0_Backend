using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Enterprises.Queries.GetActiveTerms;

public class GetActiveTermsForEnterpriseResponse
{
    public List<ActiveTermTimelineResponse> Terms { get; set; } = new();
}

public class ActiveTermTimelineResponse
{
    public Guid TermId { get; set; }
    public string TermName { get; set; } = null!;
    public Guid UniversityId { get; set; }
    public string UniversityName { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Always Active because only ongoing terms are returned.</summary>
    public TermDisplayStatus Status { get; set; } = TermDisplayStatus.Active;

    // Timeline
    public int TotalDays { get; set; }
    public int DaysElapsed { get; set; }
    public int DaysRemaining { get; set; }
    public double ProgressPercent { get; set; }

    /// <summary>
    /// List of evaluation/grading deadlines (EvaluationCycles) for the term.
    /// Empty if not yet configured by Uni Admin.
    /// </summary>
    public List<DeadlineInfo> Deadlines { get; set; } = new();
}

public class DeadlineInfo
{
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = null!;

    /// <summary>Deadline = EndDate of the EvaluationCycle.</summary>
    public DateTime DeadlineDate { get; set; }

    /// <summary>Number of days remaining until the deadline. Negative means overdue.</summary>
    public int DaysUntilDeadline { get; set; }

    /// <summary>True if there are 7 days or fewer remaining (and not overdue).</summary>
    public bool IsWarning { get; set; }

    /// <summary>True if the deadline has passed.</summary>
    public bool IsOverdue { get; set; }

    public EvaluationCycleStatus CycleStatus { get; set; }
}
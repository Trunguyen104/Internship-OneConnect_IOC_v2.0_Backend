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
    public TermDisplayStatus Status { get; set; }
    public int TotalDays { get; set; }
    public int DaysElapsed { get; set; }
    public int DaysRemaining { get; set; }
    public double ProgressPercent { get; set; }
    public bool HasDeadlinesConfigured { get; set; }
    public List<DeadlineInfo> Deadlines { get; set; } = new();
}

public class DeadlineInfo
{
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = null!;
    public string DeadlineType { get; set; } = "EvaluationSubmission";
    public DateTime DeadlineDate { get; set; }
    public int DaysUntilDeadline { get; set; }
    public bool IsWarning { get; set; }
    public bool IsOverdue { get; set; }
    public EvaluationCycleStatus CycleStatus { get; set; }
}

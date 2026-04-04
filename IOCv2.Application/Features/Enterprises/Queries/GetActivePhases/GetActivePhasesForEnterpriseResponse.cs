using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Enterprises.Queries.GetActivePhases;

public class GetActivePhasesForEnterpriseResponse
{
    public List<ActivePhaseTimelineResponse> Phases { get; set; } = new();
}

public class ActivePhaseTimelineResponse
{
    public Guid PhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    
    public int TotalDays { get; set; }
    public int DaysElapsed { get; set; }
    public int DaysRemaining { get; set; }
    public double ProgressPercent { get; set; }

    public InternshipPhaseStatus Status { get; set; }
    public List<PhaseDeadlineInfo> Deadlines { get; set; } = new();
}

public class PhaseDeadlineInfo
{
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public DateTime DeadlineDate { get; set; }
    public int DaysUntilDeadline { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsWarning { get; set; }
    public EvaluationCycleStatus CycleStatus { get; set; }
}

using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetDashboard;

/// <summary>
/// Response for Internship Group Dashboard query.
/// </summary>
public class GetInternshipGroupDashboardResponse
{
    /// <summary>Summary statistics.</summary>
    public DashboardSummaryDto Summary { get; set; } = new();
    /// <summary>Data for Burndown chart.</summary>
    public List<BurndownDto> Burndown { get; set; } = new();
    /// <summary>Completion ratio data.</summary>
    public CompletionRatioDto CompletionRatio { get; set; } = new();
    /// <summary>Distribution of tasks by status.</summary>
    public List<TaskStatusDistributionDto> TaskStatusDistribution { get; set; } = new();
    /// <summary>Workload count by person.</summary>
    public List<WorkloadDto> WorkloadByPerson { get; set; } = new();
    /// <summary>List of violations found.</summary>
    public List<ViolationDto> StudentViolations { get; set; } = new();
}

/// <summary>
/// Dashboard summary metrics.
/// </summary>
public class DashboardSummaryDto
{
    public int TotalTasks { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
}

/// <summary>
/// Burndown data point.
/// </summary>
public class BurndownDto
{
    public string Date { get; set; } = string.Empty;
    public int Remaining { get; set; }
}

/// <summary>
/// Completion ratio metrics.
/// </summary>
public class CompletionRatioDto
{
    public int OnTime { get; set; }
    public int Overdue { get; set; }
}

/// <summary>
/// Task distribution metrics.
/// </summary>
public class TaskStatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Workload metrics per person.
/// </summary>
public class WorkloadDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Violation details.
/// </summary>
public class ViolationDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}

namespace IOCv2.Application.Features.Admin.Dashboard.Queries.GetAdminDashboardStats;

public record AdminDashboardStatsResponse
{
    /// <summary>Total registered users of all roles.</summary>
    public int TotalUsers { get; init; }

    /// <summary>Total active universities in the system.</summary>
    public int TotalUniversities { get; init; }

    /// <summary>Total active enterprises in the system.</summary>
    public int TotalEnterprises { get; init; }

    /// <summary>Total jobs posted.</summary>
    public int TotalJobs { get; init; }

    /// <summary>Total students registered.</summary>
    public int TotalStudents { get; init; }

    /// <summary>Students currently on internship.</summary>
    public int ActiveInternships { get; init; }

    /// <summary>Active internship terms across all universities.</summary>
    public int ActiveTerms { get; init; }

    /// <summary>Total applications pending review (Applied or PendingAssignment).</summary>
    public int PendingApplications { get; init; }

    /// <summary>List of recent platform activities.</summary>
    public List<RecentActivityDto> RecentActivities { get; init; } = new();

    /// <summary>List of system health metrics.</summary>
    public List<SystemHealthDto> SystemHealth { get; init; } = new();
}

public record RecentActivityDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = null!;
    public string Detail { get; init; } = null!;
    public DateTime Time { get; init; }
    public string Type { get; init; } = null!; // Used by FE to determine icon
}

public record SystemHealthDto
{
    public string Label { get; init; } = null!;
    public string Value { get; init; } = null!;
    public string Status { get; init; } = null!; // "good", "warning", "critical"
}

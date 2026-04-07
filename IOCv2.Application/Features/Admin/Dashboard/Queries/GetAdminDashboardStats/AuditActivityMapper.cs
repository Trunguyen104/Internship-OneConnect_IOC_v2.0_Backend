using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.Dashboard.Queries.GetAdminDashboardStats;

/// <summary>
/// Maps <see cref="AuditLog"/> rows into dashboard-friendly activity lines.
/// </summary>
internal static class AuditActivityMapper
{
    public static RecentActivityDto Map(AuditLog a)
    {
        return new RecentActivityDto
        {
            Id = a.AuditLogId,
            Summary = BuildSummary(a.Action, a.EntityType),
            Detail = BuildDetail(a),
            Time = a.CreatedAt,
            Category = MapCategory(a.EntityType),
            ActorName = a.PerformedBy?.FullName,
            ActorEmail = a.PerformedBy?.Email,
            ActionKind = a.Action.ToString(),
            EntityId = a.EntityId,
        };
    }

    private static string BuildSummary(AuditAction action, string entityType)
    {
        var noun = HumanizeEntityNoun(entityType);
        return action switch
        {
            AuditAction.Create => $"Created {noun}",
            AuditAction.Update => $"Updated {noun}",
            AuditAction.Delete => $"Deleted {noun}",
            AuditAction.Approve => $"Approved {noun}",
            AuditAction.Deactivate => $"Deactivated {noun}",
            AuditAction.Activate => $"Activated {noun}",
            AuditAction.ResetPassword => $"Password reset · {noun}",
            AuditAction.ChangeRole => $"Role change · {noun}",
            AuditAction.EmailFailure => $"Email delivery issue · {noun}",
            _ => $"{action} · {noun}",
        };
    }

    private static string HumanizeEntityNoun(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return "record";

        return entityType.Trim() switch
        {
            "University" => "university",
            "Enterprise" => "enterprise",
            "User" => "user account",
            "Student" => "student profile",
            "Job" => "job posting",
            "InternshipApplication" => "internship application",
            "Term" => "internship term",
            "InternshipGroup" => "internship group",
            "Project" => "project",
            _ => entityType,
        };
    }

    private static string BuildDetail(AuditLog a)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a.Reason))
            parts.Add(a.Reason.Trim());

        parts.Add($"Entity ID: {a.EntityId}");
        return string.Join(" · ", parts);
    }

    /// <summary>
    /// Normalized bucket for dashboard icons (matches frontend <c>getActivityStyles</c>).
    /// </summary>
    private static string MapCategory(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return "other";

        var e = entityType.Trim().ToLowerInvariant();
        if (e.Contains("university"))
            return "university";
        if (e.Contains("enterprise"))
            return "enterprise";
        if (e.Contains("student") && !e.Contains("application"))
            return "student";
        if (e.Contains("user"))
            return "user";
        if (e.Contains("job"))
            return "job";
        if (e.Contains("application"))
            return "application";
        if (e.Contains("term"))
            return "term";
        if (e.Contains("group"))
            return "group";
        if (e.Contains("project"))
            return "project";

        return "other";
    }
}

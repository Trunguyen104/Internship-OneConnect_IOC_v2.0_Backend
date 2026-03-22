namespace IOCv2.Application.Features.WorkItems.Common;

public static class WorkItemCacheKeys
{
    private const string WorkItemPrefix = "work-item";
    private const string BacklogPrefix = "backlog";

    public static string WorkItem(Guid workItemId) => $"{WorkItemPrefix}:{workItemId}";

    public static string Backlog(
        Guid projectId,
        bool backlogOnly,
        Guid? epicId,
        string? searchTerm,
        int? type,
        int? priority,
        int? status,
        Guid? assigneeId)
    {
        var epicPart = epicId?.ToString() ?? "none";
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var typePart = type?.ToString() ?? "all";
        var priorityPart = priority?.ToString() ?? "all";
        var statusPart = status?.ToString() ?? "all";
        var assigneePart = assigneeId?.ToString() ?? "all";
        return $"{BacklogPrefix}:project:{projectId}:backlogOnly:{backlogOnly}:epic:{epicPart}:search:{searchPart}:type:{typePart}:priority:{priorityPart}:status:{statusPart}:assignee:{assigneePart}";
    }

    public static string BacklogPattern(Guid projectId) => $"{BacklogPrefix}:project:{projectId}:*";
    public static string WorkItemPattern() => $"{WorkItemPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan WorkItem = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan Backlog = TimeSpan.FromMinutes(2);
    }
}

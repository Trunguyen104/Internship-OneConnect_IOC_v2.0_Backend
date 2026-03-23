namespace IOCv2.Application.Features.StakeholderIssues.Common;

public static class StakeholderIssueCacheKeys
{
    private const string IssuePrefix = "stakeholder-issue";
    private const string IssueListPrefix = "stakeholder-issues:list";

    public static string Issue(Guid issueId) =>
        $"{IssuePrefix}:{issueId}";

    public static string IssueList(
        Guid? internshipId,
        Guid? stakeholderId,
        int? status,
        string? search,
        string? orderBy,
        int pageIndex,
        int pageSize) =>
        $"{IssueListPrefix}:internship:{internshipId}:stakeholder:{stakeholderId}:status:{status}:search:{search ?? ""}:order:{orderBy ?? ""}:page:{pageIndex}:{pageSize}";

    public static string IssueListPattern(Guid? internshipId = null, Guid? stakeholderId = null)
    {
        if (stakeholderId.HasValue)
            return $"{IssueListPrefix}:internship:{internshipId}:stakeholder:{stakeholderId}:*";
        if (internshipId.HasValue)
            return $"{IssueListPrefix}:internship:{internshipId}:*";
        return $"{IssueListPrefix}:*";
    }

    public static class Expiration
    {
        public static readonly TimeSpan Issue = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan IssueList = TimeSpan.FromMinutes(5);
    }
}

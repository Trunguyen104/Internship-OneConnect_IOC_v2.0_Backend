namespace IOCv2.Application.Features.Stakeholders.Common;

public static class StakeholderCacheKeys
{
    private const string StakeholderPrefix = "stakeholder";
    private const string StakeholderListPrefix = "stakeholders:list";

    public static string Stakeholder(Guid stakeholderId) =>
        $"{StakeholderPrefix}:{stakeholderId}";

    public static string StakeholderList(
        Guid internshipId,
        string? searchTerm,
        string? sortColumn,
        string? sortOrder,
        int pageNumber,
        int pageSize) =>
        $"{StakeholderListPrefix}:internship:{internshipId}:search:{searchTerm ?? ""}:sort:{sortColumn ?? ""}:{sortOrder ?? ""}:page:{pageNumber}:{pageSize}";

    public static string StakeholderListPattern(Guid internshipId) =>
        $"{StakeholderListPrefix}:internship:{internshipId}:*";

    public static string StakeholderPattern() => $"{StakeholderPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Stakeholder = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan StakeholderList = TimeSpan.FromMinutes(5);
    }
}

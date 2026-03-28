namespace IOCv2.Application.Features.InternshipGroups.Common;

public static class InternshipGroupCacheKeys
{
    private const string GroupPrefix = "internship-group";
    private const string GroupListPrefix = "internship-groups";

    public static string Group(Guid internshipId) => $"{GroupPrefix}:{internshipId}";

    public static string GroupList(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? status,
        Guid? phaseId,
        bool includeArchived,
        Guid? enterpriseId,
        Guid userId)
    {
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var statusPart = status?.ToString() ?? "all";
        var termPart = phaseId?.ToString() ?? "all";
        var archivedPart = includeArchived.ToString().ToLowerInvariant();
        var enterprisePart = enterpriseId?.ToString() ?? "all";

        return $"{GroupListPrefix}:user:{userId}:status:{statusPart}:search:{searchPart}:term:{termPart}:archived:{archivedPart}:enterprise:{enterprisePart}:page:{pageNumber}:size:{pageSize}";
    }

    public static string GroupListPattern() => $"{GroupListPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Group = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan GroupList = TimeSpan.FromMinutes(5);
    }
}

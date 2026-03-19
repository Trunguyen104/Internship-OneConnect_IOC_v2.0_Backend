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
        Guid? universityId,
        Guid? enterpriseId)
    {
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var statusPart = status?.ToString() ?? "all";
        var universityPart = universityId?.ToString() ?? "all";
        var enterprisePart = enterpriseId?.ToString() ?? "all";

        return $"{GroupListPrefix}:status:{statusPart}:search:{searchPart}:university:{universityPart}:enterprise:{enterprisePart}:page:{pageNumber}:size:{pageSize}";
    }

    public static string GroupListPattern() => $"{GroupListPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Group = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan GroupList = TimeSpan.FromMinutes(5);
    }
}

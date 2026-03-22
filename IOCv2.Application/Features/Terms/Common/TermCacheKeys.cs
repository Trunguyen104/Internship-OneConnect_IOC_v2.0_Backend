namespace IOCv2.Application.Features.Terms.Common;

public static class TermCacheKeys
{
    private const string TermPrefix = "term";
    private const string TermListPrefix = "terms";

    public static string Term(Guid termId, Guid? universityId = null)
    {
        var univPart = universityId?.ToString() ?? "admin";
        return $"{TermPrefix}:{termId}:{univPart}";
    }

    public static string TermList(
        Guid? universityId,
        string? searchTerm,
        int? status,
        int? year,
        int pageNumber,
        int pageSize,
        string? sortColumn,
        string? sortOrder)
    {
        var univPart = universityId?.ToString() ?? "all";
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var statusPart = status?.ToString() ?? "all";
        var yearPart = year?.ToString() ?? "all";
        var sortColPart = string.IsNullOrWhiteSpace(sortColumn) ? "none" : sortColumn.ToLowerInvariant();
        var sortOrderPart = string.IsNullOrWhiteSpace(sortOrder) ? "none" : sortOrder.ToLowerInvariant();
        return $"{TermListPrefix}:university:{univPart}:search:{searchPart}:status:{statusPart}:year:{yearPart}:page:{pageNumber}:size:{pageSize}:sort:{sortColPart}:order:{sortOrderPart}";
    }

    public static string TermListPattern() => $"{TermListPrefix}:*";
    public static string TermDetailPattern() => $"{TermPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Term = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan TermList = TimeSpan.FromMinutes(5);
    }
}

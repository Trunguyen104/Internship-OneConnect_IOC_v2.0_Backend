namespace IOCv2.Application.Features.Logbooks.Common;

public static class LogbookCacheKeys
{
    private const string LogbookPrefix = "logbook";
    private const string LogbookListPrefix = "logbook:list";

    public static string Logbook(Guid logbookId) => $"{LogbookPrefix}:{logbookId}";

    public static string LogbookList(
        Guid internshipId,
        int pageNumber,
        int pageSize,
        int? status,
        string? weekFilter,
        string? sortColumn,
        string? sortOrder)
    {
        var statusPart = status?.ToString() ?? "all";
        var weekFilterPart = string.IsNullOrWhiteSpace(weekFilter) ? "all" : weekFilter.Replace(" ", string.Empty);
        var sortColPart = string.IsNullOrWhiteSpace(sortColumn) ? "none" : sortColumn.ToLowerInvariant();
        var sortOrderPart = string.IsNullOrWhiteSpace(sortOrder) ? "none" : sortOrder.ToLowerInvariant();
        return $"{LogbookListPrefix}:internship:{internshipId}:status:{statusPart}:weeks:{weekFilterPart}:page:{pageNumber}:size:{pageSize}:sort:{sortColPart}:order:{sortOrderPart}";
    }

    public static string LogbookListPattern(Guid internshipId) =>
        $"{LogbookListPrefix}:internship:{internshipId}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Logbook = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan LogbookList = TimeSpan.FromMinutes(5);
    }
}

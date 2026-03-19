namespace IOCv2.Application.Features.Projects.Common;

public static class ProjectCacheKeys
{
    private const string ProjectPrefix = "project";
    private const string ProjectListPrefix = "projects";

    public static string Project(Guid projectId) => $"{ProjectPrefix}:{projectId}";

    public static string ProjectList(
        string? searchTerm,
        int? status,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? internshipId,
        Guid? studentId,
        int pageNumber,
        int pageSize,
        string? sortColumn,
        string? sortOrder)
    {
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var statusPart = status?.ToString() ?? "all";
        var fromPart = fromDate?.ToString("yyyyMMdd") ?? "none";
        var toPart = toDate?.ToString("yyyyMMdd") ?? "none";
        var internshipPart = internshipId?.ToString() ?? "all";
        var studentPart = studentId?.ToString() ?? "all";
        var sortColumnPart = string.IsNullOrWhiteSpace(sortColumn) ? "default" : sortColumn.Trim().ToLowerInvariant();
        var sortOrderPart = string.IsNullOrWhiteSpace(sortOrder) ? "default" : sortOrder.Trim().ToLowerInvariant();

        return $"{ProjectListPrefix}:status:{statusPart}:search:{searchPart}:from:{fromPart}:to:{toPart}:internship:{internshipPart}:student:{studentPart}:page:{pageNumber}:size:{pageSize}:sort:{sortColumnPart}:order:{sortOrderPart}";
    }

    public static string ProjectListPattern() => $"{ProjectListPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Project = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan ProjectList = TimeSpan.FromMinutes(5);
    }
}

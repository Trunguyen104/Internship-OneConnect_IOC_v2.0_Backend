namespace IOCv2.Application.Features.ProjectResources.Common;

public static class ProjectResourceCacheKeys
{
    private const string ReadPrefix = "project-resource:read";
    private const string ListPrefix = "project-resource:list";

    public static string Read(Guid resourceId) => $"{ReadPrefix}:{resourceId}";

    public static string List(
        Guid? projectId,
        int pageNumber,
        int pageSize,
        string? sortColumn,
        string? sortOrder,
        int? resourceType,
        string? searchTerm,
        string? userId)
    {
        var projectPart = projectId?.ToString() ?? "all";
        var sortColumnPart = string.IsNullOrWhiteSpace(sortColumn) ? "default" : sortColumn.Trim().ToLowerInvariant();
        var sortOrderPart = string.IsNullOrWhiteSpace(sortOrder) ? "default" : sortOrder.Trim().ToLowerInvariant();
        var typePart = resourceType?.ToString() ?? "all";
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var userPart = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim().ToLowerInvariant();

        return $"{ListPrefix}:project:{projectPart}:page:{pageNumber}:size:{pageSize}:sort:{sortColumnPart}:order:{sortOrderPart}:type:{typePart}:search:{searchPart}:user:{userPart}";
    }

    public static string ListPattern(Guid? projectId)
    {
        var projectPart = projectId?.ToString() ?? "all";
        return $"{ListPrefix}:project:{projectPart}:*";
    }

    public static class Expiration
    {
        public static readonly TimeSpan Read = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan List = TimeSpan.FromMinutes(5);
    }
}

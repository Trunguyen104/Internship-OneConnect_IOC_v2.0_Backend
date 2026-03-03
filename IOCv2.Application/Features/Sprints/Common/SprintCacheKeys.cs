namespace IOCv2.Application.Features.Sprints.Common;

/// <summary>
/// Cache key generator for Sprint-related caching
/// </summary>
public static class SprintCacheKeys
{
    private const string SprintPrefix = "sprint";
    private const string SprintListPrefix = "sprints";
    
    /// <summary>
    /// Cache key for single sprint: sprint:project:{projectId}:{sprintId}
    /// </summary>
    public static string Sprint(Guid projectId, Guid sprintId) => $"{SprintPrefix}:project:{projectId}:{sprintId}";
    
    /// <summary>
    /// Cache key pattern for all sprints in a project: sprints:project:{projectId}:*
    /// </summary>
    public static string SprintListPattern(Guid projectId) => $"{SprintListPrefix}:project:{projectId}:*";
    
    /// <summary>
    /// Cache key for paginated sprint list with filters
    /// </summary>
    public static string SprintList(
        Guid projectId,
        int pageIndex,
        int pageSize,
        string? statusFilter = null,
        string? search = null,
        string? orderBy = null)
    {
        var statusPart = string.IsNullOrWhiteSpace(statusFilter) ? "all" : statusFilter;
        var searchPart = string.IsNullOrWhiteSpace(search) ? "none" : search;
        var orderPart = string.IsNullOrWhiteSpace(orderBy) ? "default" : orderBy;
        
        return $"{SprintListPrefix}:project:{projectId}:status:{statusPart}:page:{pageIndex}:size:{pageSize}:search:{searchPart}:order:{orderPart}";
    }
    
    /// <summary>
    /// Cache expiration times
    /// </summary>
    public static class Expiration
    {
        public static readonly TimeSpan Sprint = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan SprintList = TimeSpan.FromMinutes(5);
    }
}

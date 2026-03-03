namespace IOCv2.Application.Features.Epics.Common;

/// <summary>
/// Cache key generator for Epic-related caching
/// </summary>
public static class EpicCacheKeys
{
    private const string EpicPrefix = "epic";
    private const string EpicListPrefix = "epics";
    
    /// <summary>
    /// Cache key for single epic: epic:project:{projectId}:{epicId}
    /// </summary>
    public static string Epic(Guid projectId, Guid epicId) => $"{EpicPrefix}:project:{projectId}:{epicId}";
    
    /// <summary>
    /// Cache key pattern for all epics in a project: epics:project:{projectId}:*
    /// </summary>
    public static string EpicListPattern(Guid projectId) => $"{EpicListPrefix}:project:{projectId}:*";
    
    /// <summary>
    /// Cache key for paginated epic list with filters
    /// </summary>
    public static string EpicList(
        Guid projectId, 
        int pageIndex, 
        int pageSize, 
        string? search = null, 
        string? orderBy = null)
    {
        var searchPart = string.IsNullOrWhiteSpace(search) ? "none" : search;
        var orderPart = string.IsNullOrWhiteSpace(orderBy) ? "default" : orderBy;
        
        return $"{EpicListPrefix}:project:{projectId}:page:{pageIndex}:size:{pageSize}:search:{searchPart}:order:{orderPart}";
    }
    
    /// <summary>
    /// Cache expiration times
    /// </summary>
    public static class Expiration
    {
        public static readonly TimeSpan Epic = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan EpicList = TimeSpan.FromMinutes(5);
    }
}

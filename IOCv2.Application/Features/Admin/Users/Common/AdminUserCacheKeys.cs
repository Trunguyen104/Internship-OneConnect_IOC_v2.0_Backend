namespace IOCv2.Application.Features.Admin.Users.Common;

public static class AdminUserCacheKeys
{
    private const string UserPrefix = "user";
    private const string UserListPrefix = "user:list";

    public static string User(Guid userId) => $"{UserPrefix}:{userId}";

    public static string UserList(
        string? searchTerm,
        int? role,
        int? status,
        int pageNumber,
        int pageSize,
        string? sortColumn,
        string? sortOrder)
    {
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var rolePart = role?.ToString() ?? "all";
        var statusPart = status?.ToString() ?? "all";
        var sortColumnPart = string.IsNullOrWhiteSpace(sortColumn) ? "default" : sortColumn.Trim().ToLowerInvariant();
        var sortOrderPart = string.IsNullOrWhiteSpace(sortOrder) ? "default" : sortOrder.Trim().ToLowerInvariant();

        return $"{UserListPrefix}:search:{searchPart}:role:{rolePart}:status:{statusPart}:page:{pageNumber}:size:{pageSize}:sort:{sortColumnPart}:order:{sortOrderPart}";
    }

    public static string UserListPattern() => $"{UserListPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan User = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan UserList = TimeSpan.FromMinutes(5);
    }
}

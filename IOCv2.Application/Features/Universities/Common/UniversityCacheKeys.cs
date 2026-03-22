namespace IOCv2.Application.Features.Universities.Common;

public static class UniversityCacheKeys
{
    private const string UniversityPrefix = "university";
    private const string UniversityListPrefix = "universities";

    public static string University(Guid universityId) => $"{UniversityPrefix}:{universityId}";

    public static string UniversityList(int pageNumber, int pageSize, string? searchTerm, int? status)
    {
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var statusPart = status?.ToString() ?? "all";
        return $"{UniversityListPrefix}:search:{searchPart}:status:{statusPart}:page:{pageNumber}:size:{pageSize}";
    }

    public static string UniversityListPattern() => $"{UniversityListPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan University = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan UniversityList = TimeSpan.FromMinutes(15);
    }
}

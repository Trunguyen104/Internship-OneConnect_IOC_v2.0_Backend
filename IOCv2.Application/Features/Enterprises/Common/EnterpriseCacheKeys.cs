namespace IOCv2.Application.Features.Enterprises.Common;

public static class EnterpriseCacheKeys
{
    private const string EnterpriseListPrefix = "enterprises";

    public static string EnterpriseList(
        string? searchTerm,
        string? taxCode,
        string? name,
        string? industry,
        bool? isVerified,
        int? status,
        string? sortColumn,
        string? sortOrder,
        int pageNumber,
        int pageSize)
    {
        var searchPart = string.IsNullOrWhiteSpace(searchTerm) ? "none" : searchTerm.Trim().ToLowerInvariant();
        var taxPart = string.IsNullOrWhiteSpace(taxCode) ? "none" : taxCode.ToLowerInvariant();
        var namePart = string.IsNullOrWhiteSpace(name) ? "none" : name.Trim().ToLowerInvariant();
        var industryPart = string.IsNullOrWhiteSpace(industry) ? "none" : industry.ToLowerInvariant();
        var verifiedPart = isVerified?.ToString() ?? "all";
        var statusPart = status?.ToString() ?? "all";
        var sortColPart = string.IsNullOrWhiteSpace(sortColumn) ? "none" : sortColumn.ToLowerInvariant();
        var sortOrderPart = string.IsNullOrWhiteSpace(sortOrder) ? "none" : sortOrder.ToLowerInvariant();
        return $"{EnterpriseListPrefix}:search:{searchPart}:tax:{taxPart}:name:{namePart}:industry:{industryPart}:verified:{verifiedPart}:status:{statusPart}:sort:{sortColPart}:order:{sortOrderPart}:page:{pageNumber}:size:{pageSize}";
    }

    public static string EnterpriseListPattern() => $"{EnterpriseListPrefix}:*";

    private const string ActiveTermsPrefix = "enterprise-active-terms";

    public static string ActiveTerms(Guid enterpriseId, Guid enterpriseUserId, bool isMentor, Guid? universityId)
    {
        var scopePart = isMentor ? $"mentor:{enterpriseUserId}" : $"enterprise:{enterpriseId}";
        var univPart = universityId?.ToString() ?? "all";
        return $"{ActiveTermsPrefix}:{scopePart}:univ:{univPart}";
    }

    public static string ActiveTermsPattern(Guid enterpriseId) =>
        $"{ActiveTermsPrefix}:enterprise:{enterpriseId}:*";

    public static string ActiveTermsMentorPattern(Guid enterpriseUserId) =>
        $"{ActiveTermsPrefix}:mentor:{enterpriseUserId}:*";

    /// <summary>Clear all enterprise active-terms cache (used when a term is closed/updated by Uni Admin).</summary>
    public static string AllActiveTermsPattern() => $"{ActiveTermsPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan EnterpriseList = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan ActiveTerms = TimeSpan.FromMinutes(5);
    }
}

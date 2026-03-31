using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipPhases.Common;

public static class InternshipPhaseCacheKeys
{
    private const string CacheVersion = "v2";
    private const string PhasePrefix = "internship-phase";
    private const string PhaseEnterprisePrefix = "internship-phase-enterprise";
    private const string PhaseListPrefix = "internship-phases";

    /// <summary>Admin path: cache key for a phase accessed without enterprise restriction.</summary>
    public static string Phase(Guid phaseId) => $"{PhasePrefix}:{CacheVersion}:{phaseId}";

    /// <summary>
    /// BUG-F FIX: Non-admin path cache key scoped to both phaseId and enterpriseId.
    /// Prevents admin and non-admin results from sharing the same cache slot,
    /// eliminating the risk of ownership bypass through cross-role cache hits.
    /// </summary>
    public static string PhaseForEnterprise(Guid phaseId, Guid enterpriseId)
        => $"{PhaseEnterprisePrefix}:{CacheVersion}:{enterpriseId}:{phaseId}";

    public static string PhaseList(Guid? enterpriseId, InternshipPhaseLifecycleStatus? status, bool includeEnded, int pageNumber, int pageSize)
    {
        var statusPart = status?.ToString() ?? "all";
        var enterprisePart = enterpriseId?.ToString() ?? "all";
        var endedPart = includeEnded ? "with-ended" : "no-ended";
        return $"{PhaseListPrefix}:{CacheVersion}:enterprise:{enterprisePart}:status:{statusPart}:ended:{endedPart}:page:{pageNumber}:size:{pageSize}";
    }

    public static string PhaseListPattern() => $"{PhaseListPrefix}:{CacheVersion}:*";

    /// <summary>Matches both the admin Phase keys and enterprise-scoped Phase keys.</summary>
    public static string PhasePattern() => $"{PhasePrefix}:{CacheVersion}*";

    /// <summary>Matches only enterprise-scoped Phase keys (used on Update to invalidate non-admin caches).</summary>
    public static string PhaseEnterprisePattern() => $"{PhaseEnterprisePrefix}:{CacheVersion}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Phase = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan PhaseList = TimeSpan.FromMinutes(5);
    }
}

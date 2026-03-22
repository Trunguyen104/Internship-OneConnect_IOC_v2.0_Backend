namespace IOCv2.Application.Features.EvaluationCriteria.Common;

public static class EvaluationCriteriaCacheKeys
{
    private const string CriteriaListPrefix = "evaluation-criteria";

    public static string CriteriaList(Guid cycleId) => $"{CriteriaListPrefix}:cycle:{cycleId}";
    public static string CriteriaListPattern() => $"{CriteriaListPrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan CriteriaList = TimeSpan.FromMinutes(10);
    }
}

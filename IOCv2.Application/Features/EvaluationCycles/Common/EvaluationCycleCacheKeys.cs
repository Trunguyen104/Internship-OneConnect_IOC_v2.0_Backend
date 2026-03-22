namespace IOCv2.Application.Features.EvaluationCycles.Common;

public static class EvaluationCycleCacheKeys
{
    private const string CyclePrefix = "evaluation-cycle";
    private const string CycleListPrefix = "evaluation-cycles";

    public static string Cycle(Guid cycleId) => $"{CyclePrefix}:{cycleId}";
    public static string CycleList(Guid termId) => $"{CycleListPrefix}:term:{termId}";
    public static string CycleListPattern() => $"{CycleListPrefix}:*";
    public static string CyclePattern() => $"{CyclePrefix}:*";

    public static class Expiration
    {
        public static readonly TimeSpan Cycle = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan CycleList = TimeSpan.FromMinutes(5);
    }
}

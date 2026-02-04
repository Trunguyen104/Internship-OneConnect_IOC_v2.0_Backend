namespace IOCv2.Application.Interfaces
{
    public interface IRateLimiter
    {
        Task<bool> IsBlockedAsync(string key, CancellationToken ct);
        Task<int> RegisterFailAsync(string key, int limit, TimeSpan window, TimeSpan blockFor, CancellationToken ct);
        Task ResetAsync(string key, CancellationToken ct);
    }
}

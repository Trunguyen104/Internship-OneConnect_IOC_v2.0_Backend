using IOCv2.Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace IOCv2.Infrastructure.Services.RateLimiting
{
    public class RedisRateLimiter : IRateLimiter
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisRateLimiter> _logger;

        public RedisRateLimiter(IConnectionMultiplexer redis, ILogger<RedisRateLimiter> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        private static string FailKey(string key) => $"fail:{key}";
        private static string BlockKey(string key) => $"block:{key}";

        public async Task<bool> IsBlockedAsync(string key, CancellationToken ct)
        {
            try
            {
                return await _db.KeyExistsAsync(BlockKey(key));
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable while checking block key {RateLimitKey}. Falling back to not blocked.", key);
                return false;
            }
        }

        public async Task<int> RegisterFailAsync(
            string key,
            int limit,
            TimeSpan window,
            TimeSpan blockFor,
            CancellationToken ct)
        {
            try
            {
                var failKey = FailKey(key);

                // 1) tăng count (atomic)
                var count = (int)await _db.StringIncrementAsync(failKey);

                // 2) nếu là lần đầu, set TTL cho cửa sổ thời gian
                if (count == 1)
                {
                    await _db.KeyExpireAsync(failKey, window);
                }

                // 3) nếu vượt ngưỡng -> block
                if (count >= limit)
                {
                    await _db.StringSetAsync(BlockKey(key), "1", blockFor);
                }

                return count;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable while registering failed login for key {RateLimitKey}.", key);
                return 0;
            }
        }

        public async Task ResetAsync(string key, CancellationToken ct)
        {
            try
            {
                await _db.KeyDeleteAsync(FailKey(key));
                await _db.KeyDeleteAsync(BlockKey(key));
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable while resetting rate-limit key {RateLimitKey}.", key);
            }
        }
    }
}

using IOCv2.Application.Interfaces;
using StackExchange.Redis;

namespace IOCv2.Infrastructure.Services.RateLimiting
{
    public class RedisRateLimiter : IRateLimiter
    {
        private readonly IDatabase _db;

        public RedisRateLimiter(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private static string FailKey(string key) => $"fail:{key}";
        private static string BlockKey(string key) => $"block:{key}";

        public async Task<bool> IsBlockedAsync(string key, CancellationToken ct)
        {
            // StackExchange.Redis không dùng ct trực tiếp cho mọi lệnh,
            // nhưng vẫn ok về mặt logic.
            return await _db.KeyExistsAsync(BlockKey(key));
        }

        public async Task<int> RegisterFailAsync(
            string key,
            int limit,
            TimeSpan window,
            TimeSpan blockFor,
            CancellationToken ct)
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

        public async Task ResetAsync(string key, CancellationToken ct)
        {
            await _db.KeyDeleteAsync(FailKey(key));
            await _db.KeyDeleteAsync(BlockKey(key));
        }
    }
}

using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace IOCv2.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);

                if (string.IsNullOrEmpty(cachedValue))
                    return default;

                return JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
                };

                var serializedValue = JsonSerializer.Serialize(value, JsonOptions);
                await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _distributedCache.GetAsync(key, cancellationToken);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
                return false;
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var db = _connectionMultiplexer.GetDatabase();

                var keys = server.Keys(pattern: $"*{pattern}*").ToArray();

                if (keys.Length > 0)
                {
                    await db.KeyDeleteAsync(keys);
                    _logger.LogInformation("Removed {Count} keys matching pattern: {Pattern}", keys.Length, pattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
            }
        }
    }
}

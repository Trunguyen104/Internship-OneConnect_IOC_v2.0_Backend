using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    }
}

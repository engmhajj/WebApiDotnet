using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace webapi.Services
{
    public class CacheService
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetCacheAsync(
            string key,
            string value,
            CancellationToken cancellationToken = default
        )
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration,
            };

            var bytes = Encoding.UTF8.GetBytes(value);
            await _cache.SetAsync(key, bytes, options, cancellationToken);
        }

        public async Task<string?> GetCacheAsync(
            string key,
            CancellationToken cancellationToken = default
        )
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RezervBooking.Application.Abstractions;

namespace RezervBooking.Infrastructure.Redis
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(value);

            return _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            }, cancellationToken);
        }
    }
}


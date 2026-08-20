using Microsoft.Extensions.Caching.Distributed;
using ShipmentTracking.Application.Common.Interfaces;

namespace ShipmentTracking.Infrastructure.Services.Caching;

public sealed class RedisShipmentCache(IDistributedCache cache) : IShipmentCache
{
    public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        cache.GetStringAsync(key, ct);

    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default) =>
        cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, ct);

    public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);
}

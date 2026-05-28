using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ShipmentTracking.Application.Common.Interfaces;
using System.Text.Json;

namespace ShipmentTracking.Infrastructure.Services.Cache;

public sealed class RedisCacheService(
    IDistributedCache cache,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var json = await cache.GetStringAsync(key, ct);
            return json is null ? null : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET mislukt voor key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
            };
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value, JsonOptions), options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache SET mislukt voor key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache REMOVE mislukt voor key {Key}", key); }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Redis specifiek — via IServer.Keys in productie
        // Hier: no-op als fallback (werkt zonder Redis ook)
        logger.LogDebug("RemoveByPrefix aangeroepen voor prefix {Prefix}", prefix);
        await Task.CompletedTask;
    }
}

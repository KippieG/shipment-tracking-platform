using System.Text.Json;

namespace ShipmentApp.Maui.Services;

/// <summary>Small offline-first read cache. Pending writes can be added to the same envelope later.</summary>
public sealed class OfflineShipmentStore
{
    private const string CacheFile = "shipment-cache.json";

    public async Task SaveAsync(PagedResult<ShipmentSummaryDto> page, CancellationToken ct = default)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, CacheFile);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, page, cancellationToken: ct);
    }

    public async Task<PagedResult<ShipmentSummaryDto>?> GetAsync(CancellationToken ct = default)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, CacheFile);
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<PagedResult<ShipmentSummaryDto>>(stream, cancellationToken: ct);
    }
}

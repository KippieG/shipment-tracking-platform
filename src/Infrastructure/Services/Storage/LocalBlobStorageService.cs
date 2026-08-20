using ShipmentTracking.Application.Common.Interfaces;

namespace ShipmentTracking.Infrastructure.Services.Storage;

/// <summary>Development-only storage adapter used when Azure Blob Storage is not configured.</summary>
public sealed class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "shipment-tracking", "documents");

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_root);
        var safeName = Path.GetFileName(fileName);
        var relativePath = Path.Combine(Guid.NewGuid().ToString("N"), safeName);
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = File.Create(fullPath);
        await fileStream.CopyToAsync(output, ct);
        return new Uri(fullPath).AbsoluteUri;
    }

    public Task<Stream> DownloadAsync(string blobUri, CancellationToken ct = default) =>
        Task.FromResult<Stream>(File.OpenRead(new Uri(blobUri).LocalPath));

    public Task DeleteAsync(string blobUri, CancellationToken ct = default)
    {
        var path = new Uri(blobUri).LocalPath;
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}

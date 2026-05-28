using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Domain.Enums;

namespace ShipmentTracking.Application.Common.Interfaces;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Shipment?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Shipment> Items, int TotalCount)> GetPagedAsync(
        ShipmentStatus? statusFilter,
        ShipmentPriority? priorityFilter,
        string? searchTerm,
        string? createdBy,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> GetOverdueAsync(CancellationToken ct = default);
    Task<Dictionary<ShipmentStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);
    Task AddAsync(Shipment shipment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    Task DeleteAsync(Document document, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string blobUri, CancellationToken ct = default);
    Task DeleteAsync(string blobUri, CancellationToken ct = default);
    Task<bool> ExistsAsync(string blobUri, CancellationToken ct = default);
}

public interface IShipmentEventPublisher
{
    Task PublishStatusChangedAsync(Guid shipmentId, string trackingNumber, string newStatus, CancellationToken ct = default);
    Task PublishShipmentCreatedAsync(Guid shipmentId, string trackingNumber, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendShipmentConfirmationAsync(string to, string recipientName, string trackingNumber, CancellationToken ct = default);
    Task SendStatusUpdateAsync(string to, string recipientName, string trackingNumber, string newStatus, CancellationToken ct = default);
    Task SendDeliveryConfirmationAsync(string to, string recipientName, string trackingNumber, CancellationToken ct = default);
}

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    string UserEmail { get; }
    bool IsInRole(string role);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<Domain.Events.DomainEvent> events, CancellationToken ct = default);
}

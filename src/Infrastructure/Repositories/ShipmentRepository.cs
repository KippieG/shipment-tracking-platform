using Microsoft.EntityFrameworkCore;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Domain.Enums;
using ShipmentTracking.Infrastructure.Persistence;

namespace ShipmentTracking.Infrastructure.Repositories;

public sealed class ShipmentRepository(ApplicationDbContext db) : IShipmentRepository
{
    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Shipments.FindAsync([id], ct);

    public async Task<Shipment?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await db.Shipments
            .Include(s => s.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .Include(s => s.Documents.OrderByDescending(d => d.UploadedAt))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default)
        => await db.Shipments
            .Include(s => s.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber.ToUpperInvariant(), ct);

    public async Task<(IReadOnlyList<Shipment> Items, int TotalCount)> GetPagedAsync(
        ShipmentStatus? statusFilter,
        ShipmentPriority? priorityFilter,
        string? searchTerm,
        string? createdBy,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Shipments.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(s => s.Status == statusFilter.Value);

        if (priorityFilter.HasValue)
            query = query.Where(s => s.Priority == priorityFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s =>
                s.TrackingNumber.ToLower().Contains(term) ||
                s.RecipientName.ToLower().Contains(term) ||
                s.RecipientAddress.ToLower().Contains(term) ||
                s.SenderName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(createdBy))
            query = query.Where(s => s.CreatedBy == createdBy);

        if (fromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Shipment>> GetOverdueAsync(CancellationToken ct = default)
        => await db.Shipments
            .Where(s =>
                s.EstimatedDeliveryDate.HasValue &&
                s.EstimatedDeliveryDate.Value < DateTime.UtcNow &&
                s.Status != ShipmentStatus.Delivered &&
                s.Status != ShipmentStatus.Cancelled)
            .ToListAsync(ct);

    public async Task<Dictionary<ShipmentStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
        => await db.Shipments
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

    public async Task AddAsync(Shipment shipment, CancellationToken ct = default)
        => await db.Shipments.AddAsync(shipment, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

public sealed class DocumentRepository(ApplicationDbContext db) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Documents.FindAsync([id], ct);

    public async Task<IReadOnlyList<Document>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken ct = default)
        => await db.Documents
            .Where(d => d.ShipmentId == shipmentId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Document document, CancellationToken ct = default)
        => await db.Documents.AddAsync(document, ct);

    public async Task DeleteAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Remove(document);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

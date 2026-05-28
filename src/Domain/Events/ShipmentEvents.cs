namespace ShipmentTracking.Domain.Events;

/// <summary>
/// Base record voor alle domein-events.
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string RecipientName,
    string RecipientEmail) : DomainEvent;

public sealed record ShipmentStatusChangedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string PreviousStatus,
    string NewStatus,
    string RecipientEmail) : DomainEvent;

public sealed record ShipmentDeliveredEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string RecipientName,
    string RecipientEmail,
    DateTime DeliveredAt) : DomainEvent;

public sealed record DocumentUploadedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    string FileName,
    string UploadedBy) : DomainEvent;

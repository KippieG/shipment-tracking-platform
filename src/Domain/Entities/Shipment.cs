using ShipmentTracking.Domain.Enums;
using ShipmentTracking.Domain.Events;
using ShipmentTracking.Domain.Exceptions;
using ShipmentTracking.Domain.ValueObjects;

namespace ShipmentTracking.Domain.Entities;

public sealed class Shipment
{
    private static readonly Dictionary<ShipmentStatus, IReadOnlyList<ShipmentStatus>> AllowedTransitions = new()
    {
        [ShipmentStatus.Draft]          = [ShipmentStatus.Confirmed, ShipmentStatus.Cancelled],
        [ShipmentStatus.Confirmed]      = [ShipmentStatus.InTransit, ShipmentStatus.Cancelled],
        [ShipmentStatus.InTransit]      = [ShipmentStatus.OutForDelivery, ShipmentStatus.Failed],
        [ShipmentStatus.OutForDelivery] = [ShipmentStatus.Delivered, ShipmentStatus.Failed],
        [ShipmentStatus.Delivered]      = [],
        [ShipmentStatus.Failed]         = [ShipmentStatus.InTransit],
        [ShipmentStatus.Cancelled]      = [],
    };

    public Guid Id { get; private set; }
    public string TrackingNumber { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public string SenderAddress { get; private set; } = string.Empty;
    public string SenderEmail { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string RecipientAddress { get; private set; } = string.Empty;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal WeightKg { get; private set; }
    public decimal? DeclaredValueEur { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public ShipmentPriority Priority { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<ShipmentStatusHistory> _statusHistory = [];
    public IReadOnlyList<ShipmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private readonly List<Document> _documents = [];
    public IReadOnlyList<Document> Documents => _documents.AsReadOnly();

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Shipment() { }

    public static Shipment Create(
        string senderName,
        string senderAddress,
        string senderEmail,
        string recipientName,
        string recipientAddress,
        string recipientEmail,
        string description,
        decimal weightKg,
        string createdBy,
        ShipmentPriority priority = ShipmentPriority.Standard,
        decimal? declaredValueEur = null,
        DateTime? estimatedDeliveryDate = null,
        string? notes = null)
    {
        if (weightKg <= 0)
            throw new DomainException("Gewicht moet groter zijn dan 0 kg.");
        if (weightKg > 5000)
            throw new DomainException("Gewicht mag maximaal 5000 kg zijn.");
        if (estimatedDeliveryDate.HasValue && estimatedDeliveryDate.Value <= DateTime.UtcNow)
            throw new DomainException("Verwachte leveringsdatum moet in de toekomst liggen.");

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingNumber = TrackingNumber.Generate().Value,
            SenderName = senderName.Trim(),
            SenderAddress = senderAddress.Trim(),
            SenderEmail = senderEmail.Trim().ToLowerInvariant(),
            RecipientName = recipientName.Trim(),
            RecipientAddress = recipientAddress.Trim(),
            RecipientEmail = recipientEmail.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            WeightKg = weightKg,
            DeclaredValueEur = declaredValueEur,
            Status = ShipmentStatus.Draft,
            Priority = priority,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            EstimatedDeliveryDate = estimatedDeliveryDate,
            Notes = notes?.Trim()
        };

        shipment._statusHistory.Add(ShipmentStatusHistory.Create(
            shipment.Id, ShipmentStatus.Draft, "Zending aangemaakt.", createdBy));

        shipment._domainEvents.Add(new ShipmentCreatedEvent(
            shipment.Id, shipment.TrackingNumber, recipientName, recipientEmail));

        return shipment;
    }

    public void UpdateStatus(ShipmentStatus newStatus, string notes, string updatedBy)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new InvalidStatusTransitionException(Status.ToString(), newStatus.ToString());

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus == ShipmentStatus.Delivered)
            ActualDeliveryDate = DateTime.UtcNow;

        _statusHistory.Add(ShipmentStatusHistory.Create(Id, newStatus, notes, updatedBy));

        _domainEvents.Add(new ShipmentStatusChangedEvent(
            Id, TrackingNumber, previousStatus.ToString(), newStatus.ToString(), RecipientEmail));

        if (newStatus == ShipmentStatus.Delivered)
            _domainEvents.Add(new ShipmentDeliveredEvent(
                Id, TrackingNumber, RecipientName, RecipientEmail, ActualDeliveryDate!.Value));
    }

    public void UpdateDetails(
        string? description = null,
        decimal? estimatedDeliveryDate = null,
        string? notes = null)
    {
        if (Status != ShipmentStatus.Draft && Status != ShipmentStatus.Confirmed)
            throw new DomainException("Details kunnen enkel worden bijgewerkt bij Draft of Confirmed zendingen.");

        if (description is not null) Description = description.Trim();
        if (notes is not null) Notes = notes.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDocument(Document document)
    {
        _documents.Add(document);
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new DocumentUploadedEvent(Id, TrackingNumber, document.FileName, document.UploadedBy));
    }

    public void SoftDelete()
    {
        if (Status == ShipmentStatus.InTransit || Status == ShipmentStatus.OutForDelivery)
            throw new DomainException("Een zending in transit kan niet worden verwijderd.");
        if (IsDeleted)
            throw new DomainException("Zending is al verwijderd.");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool CanTransitionTo(ShipmentStatus newStatus)
        => AllowedTransitions[Status].Contains(newStatus);
}

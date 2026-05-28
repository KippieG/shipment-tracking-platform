using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Enums;
using ShipmentTracking.Domain.Exceptions;

namespace ShipmentTracking.Application.Features.Shipments.Queries.GetShipment;

public sealed record GetShipmentQuery : IRequest<ShipmentDetailDto>
{
    public Guid? ShipmentId { get; init; }
    public string? TrackingNumber { get; init; }

    public GetShipmentQuery(Guid shipmentId) => ShipmentId = shipmentId;
    public GetShipmentQuery(string trackingNumber) => TrackingNumber = trackingNumber;
}

public sealed record ShipmentDetailDto(
    Guid Id,
    string TrackingNumber,
    string SenderName,
    string SenderAddress,
    string SenderEmail,
    string RecipientName,
    string RecipientAddress,
    string RecipientEmail,
    string Description,
    decimal WeightKg,
    decimal? DeclaredValueEur,
    ShipmentStatus Status,
    string StatusLabel,
    ShipmentPriority Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy,
    DateTime? EstimatedDeliveryDate,
    DateTime? ActualDeliveryDate,
    string? Notes,
    IReadOnlyList<StatusHistoryDto> StatusHistory,
    IReadOnlyList<DocumentSummaryDto> Documents);

public sealed record StatusHistoryDto(
    ShipmentStatus Status,
    string StatusLabel,
    string Notes,
    string ChangedBy,
    DateTime ChangedAt);

public sealed record DocumentSummaryDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string UploadedBy,
    DateTime UploadedAt);

public sealed class GetShipmentHandler(IShipmentRepository repository)
    : IRequestHandler<GetShipmentQuery, ShipmentDetailDto>
{
    public async Task<ShipmentDetailDto> Handle(GetShipmentQuery query, CancellationToken ct)
    {
        var shipment = query.ShipmentId.HasValue
            ? await repository.GetByIdWithDetailsAsync(query.ShipmentId.Value, ct)
            : await repository.GetByTrackingNumberAsync(query.TrackingNumber!, ct);

        if (shipment is null)
            throw new ShipmentNotFoundException(query.ShipmentId ?? Guid.Empty);

        return new ShipmentDetailDto(
            shipment.Id, shipment.TrackingNumber,
            shipment.SenderName, shipment.SenderAddress, shipment.SenderEmail,
            shipment.RecipientName, shipment.RecipientAddress, shipment.RecipientEmail,
            shipment.Description, shipment.WeightKg, shipment.DeclaredValueEur,
            shipment.Status, shipment.Status.ToString(), shipment.Priority,
            shipment.CreatedAt, shipment.UpdatedAt, shipment.CreatedBy,
            shipment.EstimatedDeliveryDate, shipment.ActualDeliveryDate, shipment.Notes,
            shipment.StatusHistory
                .Select(h => new StatusHistoryDto(h.Status, h.Status.ToString(), h.Notes, h.ChangedBy, h.ChangedAt))
                .ToList(),
            shipment.Documents
                .Select(d => new DocumentSummaryDto(d.Id, d.FileName, d.ContentType, d.FileSizeBytes, d.UploadedBy, d.UploadedAt))
                .ToList());
    }
}

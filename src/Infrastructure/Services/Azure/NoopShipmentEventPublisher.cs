using Microsoft.Extensions.Logging;
using ShipmentTracking.Application.Common.Interfaces;

namespace ShipmentTracking.Infrastructure.Services.Azure;

/// <summary>Local-development fallback; production must configure Azure Service Bus.</summary>
public sealed class NoopShipmentEventPublisher(ILogger<NoopShipmentEventPublisher> logger) : IShipmentEventPublisher
{
    public Task PublishStatusChangedAsync(Guid shipmentId, string trackingNumber, string newStatus, CancellationToken ct = default)
    {
        logger.LogWarning("Service Bus is not configured; event for {TrackingNumber} ({Status}) was not published.", trackingNumber, newStatus);
        return Task.CompletedTask;
    }
}

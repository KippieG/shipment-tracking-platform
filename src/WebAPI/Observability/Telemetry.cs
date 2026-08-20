using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ShipmentTracking.WebAPI.Observability;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("ShipmentTracking.Api");
    public static readonly Meter Meter = new("ShipmentTracking.Api", "1.0.0");
    public static readonly Counter<long> IdempotencyReplays = Meter.CreateCounter<long>("shipment.idempotency.replays");
    public static readonly Counter<long> IdempotencyConflicts = Meter.CreateCounter<long>("shipment.idempotency.conflicts");
}

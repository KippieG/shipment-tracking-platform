using Microsoft.Extensions.Diagnostics.HealthChecks;
using ShipmentTracking.Infrastructure.Persistence;

namespace ShipmentTracking.WebAPI.HealthChecks;

public sealed class SqlServerHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await db.Database.CanConnectAsync(ct);
            return HealthCheckResult.Healthy("SQL Server is bereikbaar.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server niet bereikbaar.", ex);
        }
    }
}

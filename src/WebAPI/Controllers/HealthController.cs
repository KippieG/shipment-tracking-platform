using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShipmentTracking.WebAPI.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var report = await healthCheckService.CheckHealthAsync(ct);
        return report.Status == HealthStatus.Healthy ? Ok(new
        {
            status = "healthy",
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
        }) : StatusCode(503, new
        {
            status = "unhealthy",
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
        });
    }
}

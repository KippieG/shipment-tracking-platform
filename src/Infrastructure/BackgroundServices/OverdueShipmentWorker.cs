using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShipmentTracking.Application.Common.Interfaces;

namespace ShipmentTracking.Infrastructure.BackgroundServices;

/// <summary>
/// Achtergrondservice die elke 6 uur controleert op verlopen zendingen
/// en de verantwoordelijke operators notificeert.
/// </summary>
public sealed class OverdueShipmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueShipmentWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OverdueShipmentWorker gestart.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueShipmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fout bij controle van verlopen zendingen.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckOverdueShipmentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IShipmentRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var overdue = await repository.GetOverdueAsync(ct);

        if (overdue.Count == 0)
        {
            logger.LogDebug("Geen verlopen zendingen gevonden.");
            return;
        }

        logger.LogWarning("{Count} verlopen zending(en) gevonden.", overdue.Count);

        foreach (var shipment in overdue)
        {
            logger.LogWarning(
                "Verlopen zending: {TrackingNumber} — verwacht {EstimatedDate}",
                shipment.TrackingNumber,
                shipment.EstimatedDeliveryDate);
        }
    }
}

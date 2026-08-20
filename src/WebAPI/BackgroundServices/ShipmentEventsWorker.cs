using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using ShipmentTracking.Infrastructure.Persistence;

namespace ShipmentTracking.WebAPI.BackgroundServices;

/// <summary>Consumes status events independently from the HTTP request pipeline.</summary>
public sealed class ShipmentEventsWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ShipmentEventsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["Azure:ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("Outbox worker is idle: Azure Service Bus is not configured.");
            return;
        }

        var queue = configuration["Azure:ServiceBus:ShipmentEventsQueue"] ?? "shipment-events";
        await using var client = new ServiceBusClient(connectionString);
        await using var sender = client.CreateSender(queue);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var batch = await db.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.AttemptCount < 10)
                .OrderBy(x => x.OccurredAt).Take(25).ToListAsync(stoppingToken);

            foreach (var message in batch)
            {
                try
                {
                    await sender.SendMessageAsync(new ServiceBusMessage(message.Payload)
                    {
                        MessageId = message.Id.ToString(), Subject = message.Type, ContentType = "application/json"
                    }, stoppingToken);
                    message.MarkProcessed();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox message {MessageId} failed (attempt {Attempt})", message.Id, message.AttemptCount + 1);
                    message.MarkFailed(ex);
                }
            }
            if (batch.Count > 0) await db.SaveChangesAsync(stoppingToken);
        }
    }
}

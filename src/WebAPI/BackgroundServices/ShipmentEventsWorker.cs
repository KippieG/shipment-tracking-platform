using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ShipmentTracking.WebAPI.BackgroundServices;

/// <summary>Consumes status events independently from the HTTP request pipeline.</summary>
public sealed class ShipmentEventsWorker(IConfiguration configuration, ILogger<ShipmentEventsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["Azure:ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var queue = configuration["Azure:ServiceBus:ShipmentEventsQueue"] ?? "shipment-events";
        await using var client = new ServiceBusClient(connectionString);
        await using var processor = client.CreateProcessor(queue, new ServiceBusProcessorOptions { MaxConcurrentCalls = 4 });
        processor.ProcessMessageAsync += async args =>
        {
            var eventType = args.Message.Subject ?? "unknown";
            logger.LogInformation("Received {EventType}, message {MessageId}: {Payload}", eventType, args.Message.MessageId, args.Message.Body.ToString());
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        };
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processing error from {Source}", args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
        finally { await processor.StopProcessingAsync(CancellationToken.None); }
    }
}

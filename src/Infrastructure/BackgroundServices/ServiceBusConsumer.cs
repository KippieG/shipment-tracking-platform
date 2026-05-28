using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ShipmentTracking.Infrastructure.BackgroundServices;

/// <summary>
/// Consumes events van Azure Service Bus — bijv. externe systemen die statussen pushen.
/// </summary>
public sealed class ServiceBusConsumer(
    IConfiguration configuration,
    ILogger<ServiceBusConsumer> logger) : BackgroundService, IAsyncDisposable
{
    private readonly ServiceBusClient _client = new(
        configuration["Azure:ServiceBus:ConnectionString"] ?? string.Empty);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = configuration["Azure:ServiceBus:InboundQueue"] ?? "shipment-inbound";

        if (string.IsNullOrEmpty(configuration["Azure:ServiceBus:ConnectionString"]))
        {
            logger.LogWarning("Service Bus niet geconfigureerd — consumer gepauzeerd.");
            return;
        }

        await using var processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false
        });

        processor.ProcessMessageAsync += OnMessageAsync;
        processor.ProcessErrorAsync += OnErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Service Bus consumer gestart op queue '{Queue}'.", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
        await processor.StopProcessingAsync();
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        logger.LogInformation("Service Bus bericht ontvangen: {Body}", body);

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            var eventType = payload?["EventType"].GetString();
            logger.LogInformation("Event type: {EventType}", eventType);

            // Hier: dispatch naar MediatR handler op basis van eventType
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fout bij verwerken Service Bus bericht.");
            await args.DeadLetterMessageAsync(args.Message, "ProcessingError", ex.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus processor fout in {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}

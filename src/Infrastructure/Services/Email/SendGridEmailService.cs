using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using ShipmentTracking.Application.Common.Interfaces;

namespace ShipmentTracking.Infrastructure.Services.Email;

public sealed class SendGridEmailService(
    IConfiguration configuration,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly string _apiKey = configuration["SendGrid:ApiKey"] ?? string.Empty;
    private readonly string _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@shipmenttracking.be";
    private readonly string _fromName = configuration["SendGrid:FromName"] ?? "Shipment Tracking Platform";

    public async Task SendShipmentConfirmationAsync(
        string to, string recipientName, string trackingNumber, CancellationToken ct = default)
    {
        await SendAsync(to, recipientName,
            subject: $"Zending bevestigd — {trackingNumber}",
            html: $"""
                <h2>Hallo {recipientName},</h2>
                <p>Uw zending is bevestigd met trackingnummer <strong>{trackingNumber}</strong>.</p>
                <p>U kunt uw zending volgen via onze portal.</p>
                """, ct);
    }

    public async Task SendStatusUpdateAsync(
        string to, string recipientName, string trackingNumber, string newStatus, CancellationToken ct = default)
    {
        await SendAsync(to, recipientName,
            subject: $"Status update — {trackingNumber}",
            html: $"""
                <h2>Hallo {recipientName},</h2>
                <p>De status van uw zending <strong>{trackingNumber}</strong> is bijgewerkt naar: <strong>{newStatus}</strong>.</p>
                """, ct);
    }

    public async Task SendDeliveryConfirmationAsync(
        string to, string recipientName, string trackingNumber, CancellationToken ct = default)
    {
        await SendAsync(to, recipientName,
            subject: $"Zending afgeleverd — {trackingNumber}",
            html: $"""
                <h2>Hallo {recipientName},</h2>
                <p>Uw zending <strong>{trackingNumber}</strong> is succesvol afgeleverd. Bedankt!</p>
                """, ct);
    }

    private async Task SendAsync(
        string to, string toName, string subject, string html, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("SendGrid API key niet geconfigureerd. E-mail niet verstuurd naar {To}", to);
            return;
        }

        try
        {
            var client = new SendGridClient(_apiKey);
            var msg = MailHelper.CreateSingleEmail(
                new EmailAddress(_fromEmail, _fromName),
                new EmailAddress(to, toName),
                subject, null, html);

            var response = await client.SendEmailAsync(msg, ct);
            logger.LogInformation("E-mail verstuurd naar {To} — status {Status}", to, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fout bij versturen e-mail naar {To}", to);
        }
    }
}

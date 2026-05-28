using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Domain.Enums;

namespace ShipmentTracking.Infrastructure.Persistence.Seeders;

public sealed class DataSeeder(
    ApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedUsersAsync(ct);
        await SeedShipmentsAsync(ct);
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            logger.LogDebug("Gebruikers al aanwezig — seed overgeslagen.");
            return;
        }

        var users = new[]
        {
            User.Create("admin@shipmenttracking.be", "Admin", "User",
                passwordHasher.Hash("Admin@12345!"), "Admin"),
            User.Create("operator@shipmenttracking.be", "Operator", "Demo",
                passwordHasher.Hash("Operator@12345!"), "Operator"),
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("{Count} testgebruikers aangemaakt.", users.Length);
    }

    private async Task SeedShipmentsAsync(CancellationToken ct)
    {
        if (await db.Shipments.AnyAsync(ct))
        {
            logger.LogDebug("Zendingen al aanwezig — seed overgeslagen.");
            return;
        }

        var shipments = new[]
        {
            Shipment.Create("Acme NV", "Industrieweg 1, 9000 Gent", "dispatch@acme.be",
                "Klant A", "Kerkstraat 5, 2000 Antwerpen", "klantr.a@email.com",
                "Elektronica - fragiel", 3.5m, "admin@shipmenttracking.be",
                ShipmentPriority.Express),

            Shipment.Create("Beta BVBA", "Havenstraat 12, 8000 Brugge", "info@beta.be",
                "Klant B", "Stationslaan 8, 3000 Leuven", "klant.b@email.com",
                "Machineonderdelen", 85m, "admin@shipmenttracking.be",
                ShipmentPriority.Standard),
        };

        // Zending 1 doorheen de statussen halen
        shipments[0].UpdateStatus(ShipmentStatus.Confirmed, "Bevestigd", "admin@shipmenttracking.be");
        shipments[0].UpdateStatus(ShipmentStatus.InTransit, "Onderweg", "admin@shipmenttracking.be");

        db.Shipments.AddRange(shipments);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("{Count} voorbeeldzendingen aangemaakt.", shipments.Length);
    }
}

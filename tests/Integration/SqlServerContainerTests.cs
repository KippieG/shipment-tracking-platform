using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace ShipmentTracking.Tests.Integration;

/// <summary>Runs against SQL Server in Docker. Requires a running Docker daemon.</summary>
[Trait("Category", "Container")]
public sealed class SqlServerContainerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async Task InitializeAsync() => await _sql.StartAsync();
    public async Task DisposeAsync() => await _sql.DisposeAsync();

    [Fact]
    public async Task Shipment_is_persisted_with_its_status_history()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_sql.GetConnectionString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var shipment = Shipment.Create("Sender", "Sender address", "Recipient", "Recipient address", "Container test", 1.5m, "test-runner");
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var stored = await db.Shipments.Include(x => x.StatusHistory).SingleAsync(x => x.Id == shipment.Id);
        stored.StatusHistory.Should().ContainSingle();
        stored.TrackingNumber.Should().Be(shipment.TrackingNumber);
    }
}

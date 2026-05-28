using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShipmentTracking.Application.Features.Shipments.Commands.CreateShipment;
using ShipmentTracking.Application.Features.Shipments.Queries.GetShipments;
using ShipmentTracking.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace ShipmentTracking.Tests.Integration;

// Vervangt de echte database door een in-memory versie
public sealed class ShipmentTrackingFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Verwijder de echte DbContext registratie
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Voeg in-memory database toe
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

            // Zorg dat de DB aangemaakt is
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}

public sealed class ShipmentsEndpointTests(ShipmentTrackingFactory factory)
    : IClassFixture<ShipmentTrackingFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_CreateShipment_WithValidData_Returns201WithTrackingNumber()
    {
        // Arrange
        var command = new CreateShipmentCommand(
            "Sender NV", "Industrieweg 1, Gent",
            "Ontvanger BV", "Havenstraat 5, Antwerpen",
            "Elektronica", 8.5m);

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateShipmentResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.TrackingNumber.Should().StartWith("STP-");
    }

    [Fact]
    public async Task POST_CreateShipment_WithInvalidData_Returns400()
    {
        // Arrange — gewicht = 0 is ongeldig
        var command = new CreateShipmentCommand(
            "", "Adres", "Ontvanger", "Adres", "Pakket", 0m);

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Shipments_ReturnsPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/shipments?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ShipmentSummaryDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GET_Shipment_WithUnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/shipments/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

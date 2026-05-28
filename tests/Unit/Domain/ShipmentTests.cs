using FluentAssertions;
using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Domain.Enums;
using ShipmentTracking.Domain.Exceptions;
using Xunit;

namespace ShipmentTracking.Tests.Unit.Domain;

public sealed class ShipmentTests
{
    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldReturnShipmentWithDraftStatus()
    {
        var shipment = CreateTestShipment();

        shipment.Status.Should().Be(ShipmentStatus.Draft);
        shipment.TrackingNumber.Should().StartWith("STP-");
        shipment.StatusHistory.Should().HaveCount(1);
        shipment.DomainEvents.Should().ContainSingle(e => e is Domain.Events.ShipmentCreatedEvent);
        shipment.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5001)]
    public void Create_WithInvalidWeight_ShouldThrowDomainException(decimal weight)
    {
        var act = () => CreateTestShipment(weightKg: weight);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithPastEstimatedDelivery_ShouldThrowDomainException()
    {
        var act = () => CreateTestShipment(estimatedDelivery: DateTime.UtcNow.AddDays(-1));
        act.Should().Throw<DomainException>().WithMessage("*toekomst*");
    }

    // ── Status transitions ────────────────────────────────────────────────────

    [Theory]
    [InlineData(ShipmentStatus.Draft,          ShipmentStatus.Confirmed)]
    [InlineData(ShipmentStatus.Draft,          ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.Confirmed,      ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Confirmed,      ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.InTransit,      ShipmentStatus.OutForDelivery)]
    [InlineData(ShipmentStatus.InTransit,      ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.OutForDelivery, ShipmentStatus.Failed)]
    [InlineData(ShipmentStatus.Failed,         ShipmentStatus.InTransit)]
    public void UpdateStatus_WithValidTransition_ShouldSucceed(ShipmentStatus from, ShipmentStatus to)
    {
        var shipment = CreateShipmentWithStatus(from);
        shipment.UpdateStatus(to, "Test", "user");
        shipment.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered,  ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Cancelled,  ShipmentStatus.Confirmed)]
    [InlineData(ShipmentStatus.Draft,      ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.InTransit,  ShipmentStatus.Draft)]
    [InlineData(ShipmentStatus.Delivered,  ShipmentStatus.Cancelled)]
    public void UpdateStatus_WithInvalidTransition_ShouldThrow(ShipmentStatus from, ShipmentStatus to)
    {
        var shipment = CreateShipmentWithStatus(from);
        var act = () => shipment.UpdateStatus(to, "Test", "user");
        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void UpdateStatus_ToDelivered_ShouldSetActualDeliveryDate()
    {
        var shipment = CreateShipmentWithStatus(ShipmentStatus.OutForDelivery);
        shipment.UpdateStatus(ShipmentStatus.Delivered, "Afgeleverd", "driver");

        shipment.ActualDeliveryDate.Should().NotBeNull();
        shipment.ActualDeliveryDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateStatus_ShouldFireDomainEvents()
    {
        var shipment = CreateTestShipment();
        shipment.ClearDomainEvents();

        shipment.UpdateStatus(ShipmentStatus.Confirmed, "OK", "user");

        shipment.DomainEvents.Should().ContainSingle(e =>
            e is Domain.Events.ShipmentStatusChangedEvent);
    }

    [Fact]
    public void UpdateStatus_ToDelivered_ShouldFireDeliveredEvent()
    {
        var shipment = CreateShipmentWithStatus(ShipmentStatus.OutForDelivery);
        shipment.ClearDomainEvents();

        shipment.UpdateStatus(ShipmentStatus.Delivered, "Afgeleverd", "user");

        shipment.DomainEvents.Should().Contain(e => e is Domain.Events.ShipmentDeliveredEvent);
    }

    // ── Soft delete ───────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_WhenDraft_ShouldMarkDeleted()
    {
        var shipment = CreateTestShipment();
        shipment.SoftDelete();
        shipment.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldThrow()
    {
        var shipment = CreateTestShipment();
        shipment.SoftDelete();
        var act = () => shipment.SoftDelete();
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.OutForDelivery)]
    public void SoftDelete_WhenInTransit_ShouldThrow(ShipmentStatus status)
    {
        var shipment = CreateShipmentWithStatus(status);
        var act = () => shipment.SoftDelete();
        act.Should().Throw<DomainException>().WithMessage("*transit*");
    }

    // ── CanTransitionTo ───────────────────────────────────────────────────────

    [Fact]
    public void CanTransitionTo_ShouldReturnTrue_ForValidTransition()
    {
        var shipment = CreateTestShipment();
        shipment.CanTransitionTo(ShipmentStatus.Confirmed).Should().BeTrue();
        shipment.CanTransitionTo(ShipmentStatus.Delivered).Should().BeFalse();
    }

    // ── Documents ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddDocument_ShouldIncreaseCountAndFireEvent()
    {
        var shipment = CreateTestShipment();
        shipment.ClearDomainEvents();

        var doc = Document.Create(shipment.Id, "factuur.pdf", "application/pdf", 1024,
            "https://blob/factuur.pdf", "user");
        shipment.AddDocument(doc);

        shipment.Documents.Should().HaveCount(1);
        shipment.DomainEvents.Should().ContainSingle(e => e is Domain.Events.DocumentUploadedEvent);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Shipment CreateTestShipment(
        decimal weightKg = 10m,
        DateTime? estimatedDelivery = null) =>
        Shipment.Create(
            "Sender NV", "Industrieweg 1, Gent", "sender@nv.be",
            "Ontvanger BV", "Havenstraat 5, Antwerpen", "ontvanger@bv.be",
            "Elektronica", weightKg, "testuser",
            estimatedDeliveryDate: estimatedDelivery);

    private static Shipment CreateShipmentWithStatus(ShipmentStatus target)
    {
        var shipment = CreateTestShipment();
        var path = target switch
        {
            ShipmentStatus.Draft          => Array.Empty<ShipmentStatus>(),
            ShipmentStatus.Confirmed      => [ShipmentStatus.Confirmed],
            ShipmentStatus.Cancelled      => [ShipmentStatus.Cancelled],
            ShipmentStatus.InTransit      => [ShipmentStatus.Confirmed, ShipmentStatus.InTransit],
            ShipmentStatus.OutForDelivery => [ShipmentStatus.Confirmed, ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery],
            ShipmentStatus.Delivered      => [ShipmentStatus.Confirmed, ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered],
            ShipmentStatus.Failed         => [ShipmentStatus.Confirmed, ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery, ShipmentStatus.Failed],
            _ => throw new ArgumentException($"Onbekend: {target}")
        };
        foreach (var s in path) shipment.UpdateStatus(s, "auto", "test");
        return shipment;
    }
}

using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ShipmentTracking.Tests.Architecture;

/// <summary>
/// Architectuurtests — controleren of de dependency-regels van Clean Architecture
/// niet geschonden worden. Deze tests vangen per ongeluk toegevoegde dependencies op.
/// </summary>
public sealed class ArchitectureTests
{
    private const string DomainNamespace      = "ShipmentTracking.Domain";
    private const string ApplicationNamespace = "ShipmentTracking.Application";
    private const string InfraNamespace       = "ShipmentTracking.Infrastructure";
    private const string WebApiNamespace      = "ShipmentTracking.WebAPI";

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(Domain.Entities.Shipment).Assembly)
            .ShouldNot().HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain mag niet afhankelijk zijn van Application");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Domain.Entities.Shipment).Assembly)
            .ShouldNot().HaveDependencyOn(InfraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain mag niet afhankelijk zijn van Infrastructure");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Application.Features.Shipments.Commands
            .CreateShipment.CreateShipmentCommand).Assembly)
            .ShouldNot().HaveDependencyOn(InfraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application mag niet afhankelijk zijn van Infrastructure");
    }

    [Fact]
    public void Application_ShouldNotDependOn_WebAPI()
    {
        var result = Types.InAssembly(typeof(Application.Features.Shipments.Commands
            .CreateShipment.CreateShipmentCommand).Assembly)
            .ShouldNot().HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application mag niet afhankelijk zijn van WebAPI");
    }

    [Fact]
    public void Handlers_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(typeof(Application.Features.Shipments.Commands
            .CreateShipment.CreateShipmentHandler).Assembly)
            .That().HaveNameEndingWith("Handler")
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Alle handlers moeten sealed zijn");
    }

    [Fact]
    public void DomainEntities_ShouldNotHave_PublicSetters()
    {
        var entityTypes = typeof(Domain.Entities.Shipment).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.Contains("Entities") == true && t.IsClass && !t.IsAbstract);

        foreach (var type in entityTypes)
        {
            var publicSetters = type.GetProperties()
                .Where(p => p.SetMethod?.IsPublic == true)
                .ToList();

            publicSetters.Should().BeEmpty(
                because: $"{type.Name} mag geen publieke setters hebben (gebruik private set of init)");
        }
    }

    [Fact]
    public void Validators_ShouldBe_InApplicationLayer()
    {
        var result = Types.InAssembly(typeof(Application.Features.Shipments.Commands
            .CreateShipment.CreateShipmentValidator).Assembly)
            .That().HaveNameEndingWith("Validator")
            .Should().ResideInNamespace(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Alle validators horen in de Application-laag");
    }
}

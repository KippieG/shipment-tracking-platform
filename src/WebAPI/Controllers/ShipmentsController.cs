using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracking.Application.Features.Shipments.Commands.CreateShipment;
using ShipmentTracking.Application.Features.Shipments.Commands.DeleteShipment;
using ShipmentTracking.Application.Features.Shipments.Commands.UpdateShipmentStatus;
using ShipmentTracking.Application.Features.Shipments.Queries.GetDashboard;
using ShipmentTracking.Application.Features.Shipments.Queries.GetShipment;
using ShipmentTracking.Application.Features.Shipments.Queries.GetShipments;
using ShipmentTracking.Domain.Enums;

namespace ShipmentTracking.WebAPI.Controllers;

/// <summary>
/// Beheer van logistieke zendingen.
/// </summary>
[ApiController]
[Route("api/shipments")]
[Authorize]
[Produces("application/json")]
public sealed class ShipmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Dashboard — statistieken en recente activiteit.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await mediator.Send(new GetDashboardQuery(), ct));

    /// <summary>
    /// Gepagineerde lijst van zendingen met filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShipmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ShipmentStatus? status,
        [FromQuery] ShipmentPriority? priority,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new GetShipmentsQuery(status, priority, search, null, from, to, page, pageSize), ct));

    /// <summary>
    /// Detail van één zending inclusief statushistory en documenten.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetShipmentQuery(id), ct));

    /// <summary>
    /// Zending opzoeken via trackingnummer (publiek toegankelijk).
    /// </summary>
    [HttpGet("track/{trackingNumber}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShipmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Track(string trackingNumber, CancellationToken ct)
        => Ok(await mediator.Send(new GetShipmentQuery(trackingNumber), ct));

    /// <summary>
    /// Nieuwe zending aanmaken.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(typeof(CreateShipmentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShipmentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Status van een zending bijwerken.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "OperatorOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        await mediator.Send(new UpdateShipmentStatusCommand(id, request.NewStatus, request.Notes), ct);
        return NoContent();
    }

    /// <summary>
    /// Zending verwijderen (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteShipmentCommand(id), ct);
        return NoContent();
    }
}

public sealed record UpdateStatusRequest(ShipmentStatus NewStatus, string Notes);

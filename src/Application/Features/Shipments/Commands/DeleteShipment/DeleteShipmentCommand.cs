using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Exceptions;

namespace ShipmentTracking.Application.Features.Shipments.Commands.DeleteShipment;

public sealed record DeleteShipmentCommand(Guid ShipmentId) : IRequest;

public sealed class DeleteShipmentHandler(
    IShipmentRepository repository,
    ICacheService cache) : IRequestHandler<DeleteShipmentCommand>
{
    public async Task Handle(DeleteShipmentCommand command, CancellationToken ct)
    {
        var shipment = await repository.GetByIdAsync(command.ShipmentId, ct)
            ?? throw new ShipmentNotFoundException(command.ShipmentId);

        shipment.SoftDelete();
        await repository.SaveChangesAsync(ct);
        await cache.RemoveAsync($"shipment:{command.ShipmentId}", ct);
    }
}

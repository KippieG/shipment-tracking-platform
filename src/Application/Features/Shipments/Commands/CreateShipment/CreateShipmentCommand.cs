using FluentValidation;
using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Domain.Enums;

namespace ShipmentTracking.Application.Features.Shipments.Commands.CreateShipment;

public sealed record CreateShipmentCommand(
    string SenderName,
    string SenderAddress,
    string SenderEmail,
    string RecipientName,
    string RecipientAddress,
    string RecipientEmail,
    string Description,
    decimal WeightKg,
    ShipmentPriority Priority = ShipmentPriority.Standard,
    decimal? DeclaredValueEur = null,
    DateTime? EstimatedDeliveryDate = null,
    string? Notes = null) : IRequest<CreateShipmentResult>;

public sealed record CreateShipmentResult(Guid Id, string TrackingNumber, string Status);

public sealed class CreateShipmentValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentValidator()
    {
        RuleFor(x => x.SenderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SenderAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SenderEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RecipientAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RecipientEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("Gewicht moet groter dan 0 zijn.")
            .LessThanOrEqualTo(5000).WithMessage("Gewicht mag maximaal 5000 kg zijn.");
        RuleFor(x => x.DeclaredValueEur)
            .GreaterThan(0).When(x => x.DeclaredValueEur.HasValue)
            .WithMessage("Aangegeven waarde moet groter dan 0 zijn.");
        RuleFor(x => x.EstimatedDeliveryDate)
            .GreaterThan(DateTime.UtcNow).When(x => x.EstimatedDeliveryDate.HasValue)
            .WithMessage("Verwachte leveringsdatum moet in de toekomst liggen.");
    }
}

public sealed class CreateShipmentHandler(
    IShipmentRepository repository,
    ICurrentUserService currentUser,
    IEmailService emailService) : IRequestHandler<CreateShipmentCommand, CreateShipmentResult>
{
    public async Task<CreateShipmentResult> Handle(CreateShipmentCommand cmd, CancellationToken ct)
    {
        var shipment = Shipment.Create(
            cmd.SenderName, cmd.SenderAddress, cmd.SenderEmail,
            cmd.RecipientName, cmd.RecipientAddress, cmd.RecipientEmail,
            cmd.Description, cmd.WeightKg, currentUser.UserName,
            cmd.Priority, cmd.DeclaredValueEur, cmd.EstimatedDeliveryDate, cmd.Notes);

        await repository.AddAsync(shipment, ct);
        await repository.SaveChangesAsync(ct);

        // Bevestigingsmail (fire-and-forget — niet blocking)
        _ = emailService.SendShipmentConfirmationAsync(
            cmd.RecipientEmail, cmd.RecipientName, shipment.TrackingNumber, ct);

        return new CreateShipmentResult(shipment.Id, shipment.TrackingNumber, shipment.Status.ToString());
    }
}

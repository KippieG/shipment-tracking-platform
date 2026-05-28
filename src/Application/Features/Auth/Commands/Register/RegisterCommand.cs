using FluentValidation;
using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Entities;
using ShipmentTracking.Domain.Exceptions;

namespace ShipmentTracking.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<RegisterResult>;

public sealed record RegisterResult(Guid UserId, string Email, string FullName);

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress()
            .WithMessage("Geldig e-mailadres is verplicht.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Wachtwoord moet minimaal 8 tekens bevatten.")
            .Matches(@"[A-Z]").WithMessage("Wachtwoord moet minstens één hoofdletter bevatten.")
            .Matches(@"[0-9]").WithMessage("Wachtwoord moet minstens één cijfer bevatten.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Wachtwoord moet minstens één speciaal teken bevatten.");

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand command, CancellationToken ct)
    {
        if (await userRepository.EmailExistsAsync(command.Email, ct))
            throw new DomainException($"Een gebruiker met e-mail '{command.Email}' bestaat al.");

        var hash = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, command.FirstName, command.LastName, hash);

        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        return new RegisterResult(user.Id, user.Email, user.FullName);
    }
}

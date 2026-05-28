using FluentValidation;
using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Exceptions;

namespace ShipmentTracking.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    string UserId,
    string FullName,
    string Role);

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, ct)
            ?? throw new UnauthorizedException("Ongeldige inloggegevens.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is gedeactiveerd.");

        if (user.IsLocked)
            throw new UnauthorizedException("Account is tijdelijk geblokkeerd wegens te veel mislukte pogingen.");

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await userRepository.SaveChangesAsync(ct);
            throw new UnauthorizedException("Ongeldige inloggegevens.");
        }

        user.RecordLogin();
        await userRepository.SaveChangesAsync(ct);

        var (token, expiresAt) = jwtTokenService.GenerateToken(
            user.Id.ToString(), user.FullName, user.Email, [user.Role]);

        return new LoginResult(token, expiresAt, user.Id.ToString(), user.FullName, user.Role);
    }
}

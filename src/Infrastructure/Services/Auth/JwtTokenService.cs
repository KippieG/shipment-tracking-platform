using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShipmentTracking.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShipmentTracking.Infrastructure.Services.Auth;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly string _secret = configuration["JwtSettings:Secret"]
        ?? throw new InvalidOperationException("JWT Secret ontbreekt.");
    private readonly string _issuer = configuration["JwtSettings:Issuer"] ?? "ShipmentTrackingPlatform";
    private readonly string _audience = configuration["JwtSettings:Audience"] ?? "ShipmentTrackingClients";
    private readonly int _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "60");

    public (string Token, DateTime ExpiresAt) GenerateToken(
        string userId, string fullName, string email, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

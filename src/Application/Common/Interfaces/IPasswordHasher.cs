namespace ShipmentTracking.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        string userId, string fullName, string email, IEnumerable<string> roles);
}

using Microsoft.AspNetCore.Identity;

namespace ShipmentTracking.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

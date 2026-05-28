namespace ShipmentTracking.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    private User() { }

    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        string role = "Operator") => new()
    {
        Id = Guid.NewGuid(),
        Email = email.Trim().ToLowerInvariant(),
        FirstName = firstName.Trim(),
        LastName = lastName.Trim(),
        PasswordHash = passwordHash,
        Role = role,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void RecordLogin() 
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

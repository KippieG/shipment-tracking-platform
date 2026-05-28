using Microsoft.EntityFrameworkCore;
using ShipmentTracking.Domain.Entities;

namespace ShipmentTracking.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistories => Set<ShipmentStatusHistory>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Globale soft-delete filter
        modelBuilder.Entity<Shipment>().HasQueryFilter(s => !s.IsDeleted);
        // Enkel actieve gebruikers
        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Automatisch UpdatedAt bijwerken op alle entiteiten die dat ondersteunen
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Models;

namespace TooliRent.Infrastructure.Data;

public class TooliRentDbContext : DbContext
{
    public TooliRentDbContext(DbContextOptions<TooliRentDbContext> options) : base(options) {}

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<ToolCategory> ToolCategories => Set<ToolCategory>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Soft delete filters
        b.Entity<Member>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<Tool>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<ToolCategory>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<Reservation>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<Loan>().HasQueryFilter(x => x.DeletedAtUtc == null);

        // Index & precision-exempel
        b.Entity<Member>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Tool>().Property(x => x.RentalPricePerDay).HasColumnType("decimal(18,2)");
        b.Entity<Reservation>().Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

        b.Entity<Tool>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Tools)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Reservation>()
            .HasIndex(x => new { x.ToolId, x.StartUtc, x.EndUtc });

        b.Entity<Loan>()
            .HasIndex(x => new { x.ToolId, x.CheckedOutAtUtc, x.DueAtUtc });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added)
            {
                if (e.Entity.Id == Guid.Empty) e.Entity.Id = Guid.NewGuid();
                e.Entity.CreatedAtUtc = now;
            }
            else if (e.State == EntityState.Modified)
            {
                e.Entity.UpdatedAtUtc = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
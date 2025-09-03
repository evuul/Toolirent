using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Models;        // Member, Tool, ToolCategory, Reservation, Loan, BaseEntity

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

        // Index & precision
        b.Entity<Member>().HasIndex(x => x.Email).IsUnique();

        b.Entity<Tool>(e =>
        {
            e.Property(x => x.RentalPricePerDay).HasColumnType("decimal(18,2)");
            e.HasOne(t => t.Category)
             .WithMany(c => c.Tools)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Reservation (ingen FK till Loan här, bara navigation)
        b.Entity<Reservation>(e =>
        {
            e.HasOne(r => r.Tool)
             .WithMany(t => t.Reservations)
             .HasForeignKey(r => r.ToolId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Member)
             .WithMany(m => m.Reservations)
             .HasForeignKey(r => r.MemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ToolId, x.StartUtc, x.EndUtc });
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
        });

        // Loan äger FK:n ReservationId → entydig 1–1
        b.Entity<Loan>(e =>
        {
            e.HasOne(l => l.Tool)
             .WithMany(t => t.Loans)
             .HasForeignKey(l => l.ToolId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(l => l.Member)
             .WithMany() // eller .WithMany(m => m.Loans) om du har navigationen
             .HasForeignKey(l => l.MemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(l => l.Reservation)
             .WithOne(r => r.Loan)
             .HasForeignKey<Loan>(l => l.ReservationId) // Loan är dependent, har FK
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.ToolId, x.CheckedOutAtUtc, x.DueAtUtc });
            e.Property(x => x.LateFee).HasColumnType("decimal(18,2)");
        });
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
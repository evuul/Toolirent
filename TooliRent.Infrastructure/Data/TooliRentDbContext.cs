using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Models; // Member, Tool, ToolCategory, Reservation, ReservationItem, Loan, LoanItem, BaseEntity

namespace TooliRent.Infrastructure.Data;

public class TooliRentDbContext : DbContext
{
    public TooliRentDbContext(DbContextOptions<TooliRentDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<ToolCategory> ToolCategories => Set<ToolCategory>();

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanItem> LoanItems => Set<LoanItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // -------------------------
        // Soft delete filters
        // -------------------------
        b.Entity<Member>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<Tool>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<ToolCategory>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<Reservation>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<ReservationItem>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<Loan>().HasQueryFilter(x => x.DeletedAtUtc == null);
        b.Entity<LoanItem>().HasQueryFilter(x => x.DeletedAtUtc == null);

        // -------------------------
        // Member
        // -------------------------
        b.Entity<Member>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
        });

        // -------------------------
        // ToolCategory
        // -------------------------
        b.Entity<ToolCategory>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
        });

        // -------------------------
        // Tool & Category
        // -------------------------
        b.Entity<Tool>(e =>
        {
            e.Property(x => x.RentalPricePerDay).HasColumnType("decimal(18,2)");

            e.HasOne(t => t.Category)
             .WithMany(c => c.Tools)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            // Vanliga listfilter
            e.HasIndex(x => x.CategoryId);
            e.HasIndex(x => x.IsAvailable);
        });

        // -------------------------
        // Reservation (multi-item)
        // -------------------------
        b.Entity<Reservation>(e =>
        {
            e.HasOne(r => r.Member)
             .WithMany(m => m.Reservations)
             .HasForeignKey(r => r.MemberId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(r => r.Items)
             .WithOne(ri => ri.Reservation)
             .HasForeignKey(ri => ri.ReservationId)
             .OnDelete(DeleteBehavior.Cascade);

            // 1–1 till Loan (Loan bär FK)
            e.HasOne(r => r.Loan)
             .WithOne(l => l.Reservation);

            // Belopp
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

            // Hjälpindex: mina/aktiva/historik
            e.HasIndex(x => new { x.MemberId, x.StartUtc, x.EndUtc });
            e.HasIndex(x => new { x.Status, x.EndUtc });
            e.HasIndex(x => x.Status);

            // Datumkontroll
            e.ToTable(tb =>
                tb.HasCheckConstraint("CK_Reservation_Dates", "[EndUtc] > [StartUtc]")
            );
        });

        // -------------------------
        // ReservationItem
        // -------------------------
        b.Entity<ReservationItem>(e =>
        {
            e.Property(x => x.PricePerDay).HasColumnType("decimal(18,2)");

            // Index för availability & joins
            e.HasIndex(x => x.ToolId);
            e.HasIndex(x => x.ReservationId);
            e.HasIndex(x => new { x.ReservationId, x.ToolId }).IsUnique();

            e.HasOne(ri => ri.Tool)
             .WithMany() // eller .WithMany(t => t.ReservationItems) om du lägger navigation
             .HasForeignKey(ri => ri.ToolId)
             .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(tb =>
                tb.HasCheckConstraint("CK_ReservationItem_PricePerDay_Positive", "[PricePerDay] >= 0")
            );
        });

        // -------------------------
        // Loan (multi-item)
        // -------------------------
        b.Entity<Loan>(e =>
        {
            e.HasOne(l => l.Member)
             .WithMany() // eller .WithMany(m => m.Loans) om du lägger navigation
             .HasForeignKey(l => l.MemberId)
             .OnDelete(DeleteBehavior.Restrict);

            // 1–1 mot Reservation (Loan är dependent och har FK)
            e.HasOne(l => l.Reservation)
             .WithOne(r => r.Loan)
             .HasForeignKey<Loan>(l => l.ReservationId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(l => l.Items)
             .WithOne(li => li.Loan)
             .HasForeignKey(li => li.LoanId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.LateFee).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

            // Vanliga listfilter
            e.HasIndex(x => new { x.MemberId, x.CheckedOutAtUtc, x.DueAtUtc });
            e.HasIndex(x => new { x.Status, x.DueAtUtc });
            e.HasIndex(x => x.Status);

            // Datumkontroll
            e.ToTable(tb =>
                tb.HasCheckConstraint("CK_Loan_Dates", "[DueAtUtc] > [CheckedOutAtUtc]")
            );
        });

        // -------------------------
        // LoanItem
        // -------------------------
        b.Entity<LoanItem>(e =>
        {
            e.Property(x => x.PricePerDay).HasColumnType("decimal(18,2)");

            // Index för availability & joins
            e.HasIndex(x => x.ToolId);
            e.HasIndex(x => x.LoanId);
            e.HasIndex(x => new { x.LoanId, x.ToolId }).IsUnique();

            e.HasOne(li => li.Tool)
             .WithMany() // eller .WithMany(t => t.LoanItems) om du lägger navigation
             .HasForeignKey(li => li.ToolId)
             .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(tb =>
                tb.HasCheckConstraint("CK_LoanItem_PricePerDay_Positive", "[PricePerDay] >= 0")
            );
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
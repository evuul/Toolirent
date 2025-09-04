using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

// IUnitOfWork → Samlar repositories, så vi kan jobba i en transaktion.
// Exempel: skapa en reservation + skapa ett lån +
// uppdatera verktygets status → allt sparas på en gång via CompleteAsync().
// IDisposable är ett inbyggt interface i .NET som gör att UnitOfWork
// kan stänga ner DbContext på ett säkert sätt
// vilket frigör resurser och stänger databaskopplingar.
public interface IUnitOfWork : IDisposable
{
    IToolRepository Tools { get; }
    IToolCategoryRepository ToolCategories { get; }
    IReservationRepository Reservations { get; }
    ILoanRepository Loans { get; }
    IMemberRepository Members { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
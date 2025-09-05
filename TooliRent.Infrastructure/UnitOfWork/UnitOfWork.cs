using TooliRent.Core.Interfaces;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly TooliRentDbContext _db;

    public UnitOfWork(
        TooliRentDbContext db,
        IToolRepository tools,
        IToolCategoryRepository toolCategories,
        IReservationRepository reservations,
        ILoanRepository loans,
        IMemberRepository members)
    {
        _db = db;
        Tools = tools;
        ToolCategories = toolCategories;
        Reservations = reservations;
        Loans = loans;
        Members = members;
    }

    public IToolRepository Tools { get; }
    public IToolCategoryRepository ToolCategories { get; }
    public IReservationRepository Reservations { get; }
    public ILoanRepository Loans { get; }
    public IMemberRepository Members { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
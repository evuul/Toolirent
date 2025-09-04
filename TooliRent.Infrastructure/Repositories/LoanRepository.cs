using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.Repositories;

public class LoanRepository : Repository<Loan>, ILoanRepository
{
    private readonly TooliRentDbContext _db;
    public LoanRepository(TooliRentDbContext db) : base(db) => _db = db;

    public override async Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Loans
            .Include(l => l.Tool)
            .Include(l => l.Member)
            .Include(l => l.Reservation)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IEnumerable<Loan>> GetOpenByMemberAsync(Guid memberId, CancellationToken ct = default)
        => await _db.Loans.AsNoTracking()
            .Where(l => l.MemberId == memberId && l.Status == LoanStatus.Open)
            .Include(l => l.Tool)
            .OrderByDescending(l => l.CheckedOutAtUtc)
            .ToListAsync(ct);

    public async Task<bool> ToolIsLoanedNowAsync(Guid toolId, CancellationToken ct = default)
        => await _db.Loans.AsNoTracking()
            .AnyAsync(l => l.ToolId == toolId && l.Status == LoanStatus.Open, ct);

    public async Task<IEnumerable<Loan>> GetOverdueAsync(DateTime asOfUtc, CancellationToken ct = default)
        => await _db.Loans.AsNoTracking()
            .Where(l => l.Status == LoanStatus.Open && l.DueAtUtc < asOfUtc)
            .Include(l => l.Tool)
            .Include(l => l.Member)
            .ToListAsync(ct);

    public async Task<(IEnumerable<Loan> Items, int TotalCount)> SearchAsync(
        Guid? memberId,
        Guid? toolId,
        LoanStatus? status,
        bool openOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var q = _db.Loans.AsNoTracking()
            .Include(l => l.Tool)
            .Include(l => l.Member)
            .Where(l => l.DeletedAtUtc == null);

        if (memberId.HasValue)
            q = q.Where(l => l.MemberId == memberId.Value);

        if (toolId.HasValue)
            q = q.Where(l => l.ToolId == toolId.Value);

        if (status.HasValue)
            q = q.Where(l => l.Status == status.Value);

        if (openOnly)
            q = q.Where(l => l.Status == LoanStatus.Open);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(l => l.CheckedOutAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
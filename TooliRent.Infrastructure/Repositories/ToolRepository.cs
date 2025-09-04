using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.Repositories;

public class ToolRepository : Repository<Tool>, IToolRepository
{
    private readonly TooliRentDbContext _db;

    public ToolRepository(TooliRentDbContext db) : base(db) => _db = db;

    public override async Task<Tool?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Tools
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await _db.Tools.AsNoTracking()
            .Where(t => t.CategoryId == categoryId)
            .Include(t => t.Category)
            .ToListAsync(ct);

    public async Task<(IEnumerable<Tool> Items, int TotalCount)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var q = _db.Tools.AsNoTracking()
                         .Where(t => t.DeletedAtUtc == null);

        if (categoryId.HasValue)
            q = q.Where(t => t.CategoryId == categoryId.Value);

        if (isAvailable.HasValue)
            q = q.Where(t => t.IsAvailable == isAvailable.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = $"%{query}%";
            q = q.Where(t => EF.Functions.Like(t.Name, like) ||
                             EF.Functions.Like(t.Description, like));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .Include(t => t.Category)
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        // Tillgängligt om:
        // - Tool är markerat som IsAvailable
        // - INGA aktiva reservationer överlappar fönstret
        // - INGA öppna lån överlappar fönstret
        var q = _db.Tools.AsNoTracking()
            .Where(t => t.IsAvailable && t.DeletedAtUtc == null);

        q = q.Where(t =>
            !_db.Reservations.Any(r =>
                r.ToolId == t.Id &&
                r.Status == Core.Enums.ReservationStatus.Active &&
                r.StartUtc < toUtc &&
                r.EndUtc   > fromUtc)
            &&
            !_db.Loans.Any(l =>
                l.ToolId == t.Id &&
                l.Status == Core.Enums.LoanStatus.Open &&
                l.CheckedOutAtUtc < toUtc &&
                (l.ReturnedAtUtc == null || l.ReturnedAtUtc > fromUtc))
        );

        return await q.Include(t => t.Category).ToListAsync(ct);
    }
}
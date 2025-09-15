using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.Repositories;

public class ToolRepository : Repository<Tool>, IToolRepository
{
    private readonly TooliRentDbContext _db;

    public ToolRepository(TooliRentDbContext db) : base(db) => _db = db;

    public override async Task<Tool?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Tools
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await _db.Tools.AsNoTracking()
            .Where(t => t.CategoryId == categoryId && t.DeletedAtUtc == null)
            .Include(t => t.Category)
            .OrderBy(t => t.Name)
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

    /// <summary>
    /// Hämtar alla verktyg som är tillgängliga i intervallet [fromUtc, toUtc).
    /// Villkor:
    ///  - Tool.IsAvailable = true
    ///  - Ingen aktiv reservation överlappar
    ///  - Inget öppet (eller ej återlämnat) lån överlappar
    /// </summary>
    public async Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        // Säkerhetsnät (låter även SQL kortsluta bra)
        if (toUtc <= fromUtc)
            return Enumerable.Empty<Tool>();

        var q = _db.Tools
            .AsNoTracking()
            .Where(t => t.DeletedAtUtc == null && t.IsAvailable);

        q = q.Where(t =>
            // INGA aktiva reservationer som överlappar
            !_db.Reservations.Any(r =>
                r.ToolId == t.Id &&
                r.Status == ReservationStatus.Active &&
                r.StartUtc < toUtc &&
                r.EndUtc   > fromUtc)
            &&
            // INGA lån som överlappar (öppna, eller ej återlämnade)
            !_db.Loans.Any(l =>
                l.ToolId == t.Id &&
                (l.Status == LoanStatus.Open || l.ReturnedAtUtc == null) &&
                l.CheckedOutAtUtc < toUtc &&
                ((l.ReturnedAtUtc ?? l.DueAtUtc) > fromUtc))
        );

        return await q
            .Include(t => t.Category)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Snabb ja/nej-koll om ett visst verktyg är tillgängligt i intervallet [fromUtc, toUtc).
    /// (Valfritt att använda – bra för validering i t.ex. ReservationService)
    /// </summary>
    public async Task<bool> IsAvailableInWindowAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        if (toUtc <= fromUtc) return false;

        // Börja med verktyget måste finnas och vara IsAvailable
        var baseOk = await _db.Tools
            .AsNoTracking()
            .AnyAsync(t => t.Id == toolId && t.DeletedAtUtc == null && t.IsAvailable, ct);

        if (!baseOk) return false;

        var hasReservationOverlap = await _db.Reservations
            .AsNoTracking()
            .AnyAsync(r =>
                r.ToolId == toolId &&
                r.Status == ReservationStatus.Active &&
                r.StartUtc < toUtc &&
                r.EndUtc   > fromUtc, ct);

        if (hasReservationOverlap) return false;

        var hasLoanOverlap = await _db.Loans
            .AsNoTracking()
            .AnyAsync(l =>
                l.ToolId == toolId &&
                (l.Status == LoanStatus.Open || l.ReturnedAtUtc == null) &&
                l.CheckedOutAtUtc < toUtc &&
                ((l.ReturnedAtUtc ?? l.DueAtUtc) > fromUtc), ct);

        return !hasLoanOverlap;
    }
    
    public async Task<bool> IsAvailableInWindowIgnoringAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? ignoreReservationId = null,
        Guid? ignoreLoanId = null,
        CancellationToken ct = default)
    {
        // Verktyget måste finnas och vara markerat som tillgängligt (ej soft-deletat)
        var tool = await _db.Tools
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == toolId && t.DeletedAtUtc == null, ct);

        if (tool is null || !tool.IsAvailable) return false;

        // Finns någon ANNAN aktiv reservation som krockar?
        var reservationOverlap = await _db.Reservations.AsNoTracking().AnyAsync(r =>
                r.ToolId == toolId &&
                r.Status == Core.Enums.ReservationStatus.Active &&
                r.StartUtc < toUtc &&
                r.EndUtc > fromUtc &&
                (ignoreReservationId == null || r.Id != ignoreReservationId),
            ct);

        if (reservationOverlap) return false;

        // Finns något ANNAT öppet lån som krockar?
        var loanOverlap = await _db.Loans.AsNoTracking().AnyAsync(l =>
                l.ToolId == toolId &&
                l.Status == Core.Enums.LoanStatus.Open &&
                l.CheckedOutAtUtc < toUtc &&
                (l.ReturnedAtUtc == null || l.ReturnedAtUtc > fromUtc) &&
                (ignoreLoanId == null || l.Id != ignoreLoanId),
            ct);

        return !loanOverlap;
    }
}
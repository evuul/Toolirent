// TooliRent.Infrastructure/Repositories/ToolRepository.cs
using System.Linq; // Where/OrderBy/Any/Distinct
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

    // ---------------------------------------------------------------------
    // Browsing / Listing
    // ---------------------------------------------------------------------

    public async Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await _db.Tools.AsNoTracking()
            .Where(t => t.DeletedAtUtc == null && t.CategoryId == categoryId)
            .Include(t => t.Category)                         // <- laddar kategori
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    /// <summary>
    /// Sök/lista verktyg.
    /// Om isAvailable == true filtreras "tillgänglig JUST NU":
    ///  - Inget öppet lån (Loan.Status == Open)
    ///  - Ingen aktiv reservation som överlappar nu
    ///  - Tool.IsAvailable (manuell "ur bruk"-flagga) måste vara true
    /// </summary>
    public async Task<(IEnumerable<Tool> Items, int TotalCount)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var now = DateTime.UtcNow;

        var q = _db.Tools.AsNoTracking()
            .Where(t => t.DeletedAtUtc == null);

        if (categoryId is Guid cid && cid != Guid.Empty)
            q = q.Where(t => t.CategoryId == cid);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(t =>
                EF.Functions.Like(t.Name, $"%{term}%") ||
                (t.Description != null && EF.Functions.Like(t.Description, $"%{term}%")));
        }

        if (isAvailable.HasValue)
        {
            if (isAvailable.Value)
            {
                // Tillgänglig JUST NU = manuell flagga OCH inte blockerad av lån/reservation nu
                q = q.Where(t => t.IsAvailable)
                     .Where(t =>
                        // Inga öppna lån som håller verktyget
                        !_db.LoanItems.Any(li =>
                            li.ToolId == t.Id &&
                            li.Loan.Status == LoanStatus.Open)
                        &&
                        // Ingen aktiv reservation som pågår nu
                        !_db.ReservationItems.Any(ri =>
                            ri.ToolId == t.Id &&
                            ri.Reservation.Status == ReservationStatus.Active &&
                            now < ri.Reservation.EndUtc &&
                            now >= ri.Reservation.StartUtc)
                     );
            }
            else
            {
                // Inte tillgänglig nu = manuell spärr ELLER upptagen av lån/reservation just nu
                q = q.Where(t =>
                        !t.IsAvailable
                        || _db.LoanItems.Any(li =>
                            li.ToolId == t.Id &&
                            li.Loan.Status == LoanStatus.Open)
                        || _db.ReservationItems.Any(ri =>
                            ri.ToolId == t.Id &&
                            ri.Reservation.Status == ReservationStatus.Active &&
                            now < ri.Reservation.EndUtc &&
                            now >= ri.Reservation.StartUtc)
                    );
            }
        }

        // Ladda kategori så CategoryName i DTO inte blir null
        q = q.Include(t => t.Category);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // ---------------------------------------------------------------------
    // Availability (READ)
    // ---------------------------------------------------------------------

    /// <summary>
    /// "Lediga i fönster" = manuell flagga (IsAvailable) OCH
    /// ingen överlapp mot öppna lån / aktiva reservationer i fönstret.
    /// </summary>
    public async Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        var q = _db.Tools.AsNoTracking()
            .Where(t => t.DeletedAtUtc == null && t.IsAvailable)
            .Where(t =>
                // överlappning: [CheckedOut, Due) vs [from, to)
                !_db.LoanItems.Any(li =>
                    li.ToolId == t.Id &&
                    li.Loan.Status == LoanStatus.Open &&
                    fromUtc < li.Loan.DueAtUtc &&
                    toUtc   > li.Loan.CheckedOutAtUtc)
                &&
                // överlappning: [ResStart, ResEnd) vs [from, to)
                !_db.ReservationItems.Any(ri =>
                    ri.ToolId == t.Id &&
                    ri.Reservation.Status == ReservationStatus.Active &&
                    fromUtc < ri.Reservation.EndUtc &&
                    toUtc   > ri.Reservation.StartUtc)
            )
            .Include(t => t.Category);

        return await q
            .OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToListAsync(ct);
    }

    public async Task<bool> IsAvailableInWindowAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        // manuellt ur bruk eller saknas?
        var tool = await _db.Tools.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == toolId && t.DeletedAtUtc == null, ct);

        if (tool is null) return false;
        if (!tool.IsAvailable) return false;

        var hasOpenLoanOverlap = await _db.LoanItems
            .AnyAsync(li =>
                li.ToolId == toolId &&
                li.Loan.Status == LoanStatus.Open &&
                fromUtc < li.Loan.DueAtUtc &&
                toUtc   > li.Loan.CheckedOutAtUtc, ct);

        if (hasOpenLoanOverlap) return false;

        var hasActiveReservationOverlap = await _db.ReservationItems
            .AnyAsync(ri =>
                ri.ToolId == toolId &&
                ri.Reservation.Status == ReservationStatus.Active &&
                fromUtc < ri.Reservation.EndUtc &&
                toUtc   > ri.Reservation.StartUtc, ct);

        return !hasActiveReservationOverlap;
    }

    public async Task<IDictionary<Guid, bool>> AreAvailableInWindowAsync(
        IEnumerable<Guid> toolIds,
        DateTime startUtc,
        DateTime endUtc,
        Guid? ignoreReservationId = null,
        Guid? ignoreLoanId = null,
        CancellationToken ct = default)
    {
        var ids = toolIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, bool>();

        // Flaggbaserad spärr (ur bruk / soft delete) → false direkt
        var blockedFlags = await _db.Tools.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new { t.Id, Flag = (t.DeletedAtUtc != null || !t.IsAvailable) })
            .ToListAsync(ct);

        var result = ids.ToDictionary(id => id, _ => true);
        foreach (var b in blockedFlags.Where(x => x.Flag))
            result[b.Id] = false;

        // Öppna lån som överlappar fönstret
        var loanBlocked = await _db.LoanItems.AsNoTracking()
            .Where(li => ids.Contains(li.ToolId)
                         && li.Loan.Status == LoanStatus.Open
                         && (ignoreLoanId == null || li.LoanId != ignoreLoanId.Value)
                         && startUtc < li.Loan.DueAtUtc
                         && endUtc   > li.Loan.CheckedOutAtUtc)
            .Select(li => li.ToolId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var id in loanBlocked) result[id] = false;

        // Aktiva reservationer som överlappar fönstret
        var resBlocked = await _db.ReservationItems.AsNoTracking()
            .Where(ri => ids.Contains(ri.ToolId)
                         && ri.Reservation.Status == ReservationStatus.Active
                         && (ignoreReservationId == null || ri.ReservationId != ignoreReservationId.Value)
                         && startUtc < ri.Reservation.EndUtc
                         && endUtc   > ri.Reservation.StartUtc)
            .Select(ri => ri.ToolId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var id in resBlocked) result[id] = false;

        return result;
    }

    [Obsolete("Use AreAvailableInWindowAsync(toolIds, startUtc, endUtc, ignoreReservationId, ignoreLoanId, ct).")]
    public async Task<bool> IsAvailableInWindowIgnoringAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? ignoreReservationId = null,
        Guid? ignoreLoanId = null,
        CancellationToken ct = default)
    {
        var dict = await AreAvailableInWindowAsync(new[] { toolId }, fromUtc, toUtc, ignoreReservationId, ignoreLoanId, ct);
        return dict.TryGetValue(toolId, out var ok) && ok;
    }
}
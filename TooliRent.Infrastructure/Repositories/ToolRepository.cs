// TooliRent.Infrastructure/Repositories/ToolRepository.cs
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

    // ---------- Browsing/Listing ----------

    public async Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await _db.Tools.AsNoTracking()
            .Where(t => t.CategoryId == categoryId)
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
        if (pageSize <= 0) pageSize = 20;

        var q = _db.Tools.AsNoTracking().AsQueryable();

        if (categoryId is Guid cid && cid != Guid.Empty)
            q = q.Where(t => t.CategoryId == cid);

        if (isAvailable is bool avail)
            q = q.Where(t => t.IsAvailable == avail);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(t =>
                t.Name.Contains(term) ||
                (t.Description != null && t.Description.Contains(term)));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // ---------- Availability (READ) ----------

    public async Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        // Starta från alla verktyg som är flaggade som tillgängliga
        var baseIds = await _db.Tools.AsNoTracking()
            .Where(t => t.IsAvailable)
            .Select(t => t.Id)
            .ToListAsync(ct);

        if (baseIds.Count == 0) return Enumerable.Empty<Tool>();

        // Hitta konflikter från aktiva reservationer
        var reservationConflicts = await _db.Reservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Active
                        && r.StartUtc < toUtc
                        && r.EndUtc   > fromUtc)
            .SelectMany(r => r.Items)
            .Where(ri => baseIds.Contains(ri.ToolId))
            .Select(ri => ri.ToolId)
            .Distinct()
            .ToListAsync(ct);

        // Hitta konflikter från öppna lån
        var loanConflicts = await _db.Loans.AsNoTracking()
            .Where(l => l.Status == LoanStatus.Open
                        && l.CheckedOutAtUtc < toUtc
                        && l.DueAtUtc       > fromUtc)
            .SelectMany(l => l.Items)
            .Where(li => baseIds.Contains(li.ToolId))
            .Select(li => li.ToolId)
            .Distinct()
            .ToListAsync(ct);

        var conflicts = new HashSet<Guid>(reservationConflicts);
        foreach (var x in loanConflicts) conflicts.Add(x);

        var okIds = baseIds.Where(id => !conflicts.Contains(id)).ToList();

        if (okIds.Count == 0) return Enumerable.Empty<Tool>();

        return await _db.Tools.AsNoTracking()
            .Where(t => okIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<bool> IsAvailableInWindowAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        var dict = await AreAvailableInWindowAsync(new[] { toolId }, fromUtc, toUtc, null, null, ct);
        return dict.TryGetValue(toolId, out var ok) && ok;
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

        // Konflikter från aktiva reservationer i fönstret
        var reservationConflicts = await _db.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Active
                        && r.StartUtc < endUtc
                        && r.EndUtc   > startUtc
                        && (ignoreReservationId == null || r.Id != ignoreReservationId.Value))
            .SelectMany(r => r.Items)
            .Where(ri => ids.Contains(ri.ToolId))
            .Select(ri => ri.ToolId)
            .Distinct()
            .ToListAsync(ct);

        // Konflikter från öppna lån i fönstret
        var loanConflicts = await _db.Loans
            .AsNoTracking()
            .Where(l => l.Status == LoanStatus.Open
                        && l.CheckedOutAtUtc < endUtc
                        && l.DueAtUtc       > startUtc
                        && (ignoreLoanId == null || l.Id != ignoreLoanId.Value))
            .SelectMany(l => l.Items)
            .Where(li => ids.Contains(li.ToolId))
            .Select(li => li.ToolId)
            .Distinct()
            .ToListAsync(ct);

        var conflicts = new HashSet<Guid>(reservationConflicts);
        foreach (var x in loanConflicts) conflicts.Add(x);

        // Bygg resultat: true om inte konflikt och verktyget är flaggat som tillgängligt
        var availableFlags = await _db.Tools.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new { t.Id, t.IsAvailable })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, bool>(ids.Length);
        var flagMap = availableFlags.ToDictionary(a => a.Id, a => a.IsAvailable);

        foreach (var id in ids)
        {
            var flagOk = flagMap.TryGetValue(id, out var isAvailFlag) ? isAvailFlag : false;
            result[id] = flagOk && !conflicts.Contains(id);
        }

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
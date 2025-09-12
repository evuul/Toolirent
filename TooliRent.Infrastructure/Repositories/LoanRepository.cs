using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Core.Models.Admin;
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
    
    public async Task<(IEnumerable<Loan> Items, int Total)> AdminSearchAsync(
        DateTime? fromUtc, DateTime? toUtc, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        if (page <= 0) page = 1; if (pageSize <= 0) pageSize = 20;

        var q = _db.Loans.AsNoTracking()
            .Include(l => l.Tool).ThenInclude(t => t.Category)
            .Include(l => l.Member)
            .Where(l => l.DeletedAtUtc == null);

        if (fromUtc.HasValue) q = q.Where(l => l.CheckedOutAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)   q = q.Where(l => l.CheckedOutAtUtc <  toUtc.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<LoanStatus>(status, true, out var s))
                q = q.Where(l => l.Status == s);
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(l => l.CheckedOutAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
    
public async Task<AdminStatsResult> GetAdminStatsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
{
    var start = fromUtc ?? DateTime.UtcNow.AddDays(-30);
    var end   = toUtc   ?? DateTime.UtcNow;

    // ---------- Basqueries ----------
    // Lån som överlappar perioden (börjat före end och avslut slutar efter start)
    var loans = _db.Loans.AsNoTracking()
        .Include(l => l.Tool).ThenInclude(t => t.Category)
        .Include(l => l.Member)
        .Where(l => l.DeletedAtUtc == null &&
                    l.CheckedOutAtUtc < end &&
                    (l.ReturnedAtUtc ?? l.DueAtUtc) > start);

    // Reservationer som startar i perioden (statistik/volym – används ej för revenue)
    var reservations = _db.Reservations.AsNoTracking()
        .Include(r => r.Tool).ThenInclude(t => t.Category)
        .Include(r => r.Member)
        .Where(r => r.DeletedAtUtc == null &&
                    r.StartUtc >= start &&
                    r.StartUtc < end);

    var tools = _db.Tools.AsNoTracking().Where(t => t.DeletedAtUtc == null);

    // ---------- Hämta listor ----------
    var toolsTotal = await tools.CountAsync(ct);
    var loansList  = await loans.ToListAsync(ct);
    var resList    = await reservations.ToListAsync(ct);

    // ---------- Lånestatusar ----------
    var loansTotal    = loansList.Count;
    var loansOpen     = loansList.Count(l => l.Status == LoanStatus.Open);
    var loansReturned = loansList.Count(l => l.Status == LoanStatus.Returned);
    var loansLate     = loansList.Count(l => l.Status == LoanStatus.Late);

    // ---------- Reservationstatusar ----------
    var resTotal  = resList.Count;
    var resActive = resList.Count(r => r.Status == ReservationStatus.Active);

    // ---------- Intäkter ----------
    // 1) Beräkna lånedagar inom period per lån (clamp) * dagspris
    //    Obs: om Tool saknas/har inget pris → tolka som 0 kr/dag.
    var periodDays = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));

    decimal revenueFromLoans = 0m;
    foreach (var l in loansList)
    {
        var loanStart = l.CheckedOutAtUtc < start ? start : l.CheckedOutAtUtc;
        var loanEnd   = (l.ReturnedAtUtc ?? l.DueAtUtc) > end ? end : (l.ReturnedAtUtc ?? l.DueAtUtc);

        var days = Math.Max(0, (int)Math.Ceiling((loanEnd - loanStart).TotalDays));
        var pricePerDay = l.Tool?.RentalPricePerDay ?? 0m;

        revenueFromLoans += days * pricePerDay;
    }

    // 2) Förseningsavgifter (tas rakt av per lån i listan — de hör till lånen)
    var revenueFromLate = loansList.Sum(l => l.LateFee ?? 0m);

    // 3) Total intäkt i perioden
    var revenueTotal = revenueFromLoans + revenueFromLate;

    // ---------- Toppverktyg per antal lån ----------
    var topTools = loansList
        .GroupBy(l => new { l.ToolId, ToolName = l.Tool!.Name })
        .Select(g => new TopToolItem
        {
            ToolId     = g.Key.ToolId,
            ToolName   = g.Key.ToolName,
            LoansCount = g.Count()
        })
        .OrderByDescending(x => x.LoansCount)
        .Take(5)
        .ToList();

    // ---------- Kategoriutnyttjande ----------
    // Grov indikator: (summa lånedagar inom period) / (antal tools i kategorin * periodens dagar)
    var toolCountByCat = await tools
        .GroupBy(t => t.CategoryId)
        .Select(g => new { CategoryId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

    var utilization = loansList
        .Select(l =>
        {
            var s = l.CheckedOutAtUtc < start ? start : l.CheckedOutAtUtc;
            var e = (l.ReturnedAtUtc ?? l.DueAtUtc) > end ? end : (l.ReturnedAtUtc ?? l.DueAtUtc);
            var days = Math.Max(0, (int)Math.Ceiling((e - s).TotalDays));
            return new
            {
                CatId   = l.Tool!.CategoryId,
                CatName = l.Tool.Category!.Name,
                Days    = days
            };
        })
        .GroupBy(x => new { x.CatId, x.CatName })
        .Select(g =>
        {
            toolCountByCat.TryGetValue(g.Key.CatId, out var catToolCount);
            var denom = Math.Max(1, (catToolCount) * periodDays);
            var pct   = denom == 0 ? 0d : (double)g.Sum(x => x.Days) / denom * 100d;

            return new CategoryUtilizationItem
            {
                CategoryId     = g.Key.CatId,
                CategoryName   = g.Key.CatName,
                UtilizationPct = Math.Round(pct, 1)
            };
        })
        .OrderByDescending(x => x.UtilizationPct)
        .ToList();

    // ---------- Toppmedlemmar per antal lån ----------
    var topMembers = loansList
        .GroupBy(l => new
        {
            l.MemberId,
            MemberName = ((l.Member!.FirstName ?? string.Empty) + " " + (l.Member!.LastName ?? string.Empty)).Trim()
        })
        .Select(g => new MemberActivityItem
        {
            MemberId   = g.Key.MemberId,
            MemberName = g.Key.MemberName,
            LoansCount = g.Count()
        })
        .OrderByDescending(x => x.LoansCount)
        .Take(5)
        .ToList();

    return new AdminStatsResult
    {
        ToolsTotal          = toolsTotal,
        LoansTotal          = loansTotal,
        LoansOpen           = loansOpen,
        LoansReturned       = loansReturned,
        LoansLate           = loansLate,
        ReservationsTotal   = resTotal,
        ReservationsActive  = resActive,
        RevenueTotal        = revenueTotal,
        TopToolsByLoans     = topTools,
        CategoryUtilization = utilization,
        TopMembersByLoans   = topMembers
    };
}
}
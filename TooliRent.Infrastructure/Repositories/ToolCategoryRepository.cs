using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Interfaces.Repositories;
using TooliRent.Core.Models;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.Repositories;

public class ToolCategoryRepository : Repository<ToolCategory>, IToolCategoryRepository
{
    private readonly TooliRentDbContext _ctx;

    public ToolCategoryRepository(TooliRentDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = (name ?? string.Empty).Trim().ToUpperInvariant();

        var query = _ctx.ToolCategories.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(c => c.Name.Trim().ToUpper() == normalized, ct);
    }

    public async Task<ToolCategory?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = (name ?? string.Empty).Trim().ToLowerInvariant();
        return await _ctx.ToolCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == n, ct);
    }

    /// <summary>
    /// Hämtar kategorier tillsammans med antal verktyg (total) och antal tillgängliga (available).
    /// </summary>
    public async Task<IEnumerable<(ToolCategory Category, int Total, int Available)>> GetWithCountsAsync(
        CancellationToken ct = default)
    {
        var rows = await _ctx.ToolCategories
            .Select(c => new
            {
                Category = c,
                Total = c.Tools.Count(),
                Available = c.Tools.Count(t => t.IsAvailable && t.DeletedAtUtc == null)
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.Select(x => (x.Category, x.Total, x.Available));
    }
}
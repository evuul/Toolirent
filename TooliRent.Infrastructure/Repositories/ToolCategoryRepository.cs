using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Interfaces;
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
        var normalized = name.Trim().ToUpperInvariant();

        var query = _ctx.ToolCategories.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(c => c.Name.Trim().ToUpper() == normalized, ct);
    }
    
    public async Task<ToolCategory?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = (name ?? string.Empty).Trim().ToLower();
        return await _ctx.ToolCategories.FirstOrDefaultAsync(c => c.Name.ToLower() == n, ct);
    }
}
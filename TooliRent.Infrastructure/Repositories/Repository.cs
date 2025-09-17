using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Interfaces;

namespace TooliRent.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly DbContext _ctx;
    protected readonly DbSet<T> _set;

    public Repository(DbContext ctx)
    {
        _ctx = ctx;
        _set = _ctx.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);  // EF spårar entiteten

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
        return entity;
    }

    public virtual Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        _set.Update(entity); // markerar Modified om tracked, annars attach+Modified
        return Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null)
            _set.Remove(entity);
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct) is not null;
}
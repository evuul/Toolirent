using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class ToolService : IToolService
{
    private readonly IUnitOfWork _uow;

    public ToolService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<(IEnumerable<Tool> Items, int Total)> SearchAsync(
        Guid? categoryId, bool? isAvailable, string? query,
        int page, int pageSize, CancellationToken ct = default)
        => _uow.Tools.SearchAsync(categoryId, isAvailable, query, page, pageSize, ct);

    public Task<Tool?> GetAsync(Guid id, CancellationToken ct = default)
        => _uow.Tools.GetByIdAsync(id, ct);

    public async Task<Tool> CreateAsync(Tool tool, CancellationToken ct = default)
    {
        await _uow.Tools.AddAsync(tool, ct);
        await _uow.SaveChangesAsync(ct);
        return tool;
    }

    public async Task<bool> UpdateAsync(Tool tool, CancellationToken ct = default)
    {
        await _uow.Tools.UpdateAsync(tool, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Tools.GetByIdAsync(id, ct);
        if (entity is null) return false;
        entity.DeletedAtUtc = DateTime.UtcNow; // soft-delete
        await _uow.Tools.UpdateAsync(entity, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
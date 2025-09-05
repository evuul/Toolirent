using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class ToolCategoryService : IToolCategoryService
{
    private readonly IUnitOfWork _uow;

    public ToolCategoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IEnumerable<ToolCategory>> GetAllAsync(CancellationToken ct = default)
        => _uow.ToolCategories.GetAllAsync(ct);

    public Task<ToolCategory?> GetAsync(Guid id, CancellationToken ct = default)
        => _uow.ToolCategories.GetByIdAsync(id, ct);

    public async Task<ToolCategory> CreateAsync(ToolCategory entity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("Category name is required.", nameof(entity));

        entity.Name = entity.Name.Trim();

        await EnsureUniqueNameAsync(entity.Name, excludeId: null, ct);

        await _uow.ToolCategories.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> UpdateAsync(ToolCategory entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Category Id is required.", nameof(entity));

        var current = await _uow.ToolCategories.GetByIdAsync(entity.Id, ct);
        if (current is null) return false;

        var newName = (entity.Name ?? string.Empty).Trim();

        // kontrollera unikhet (exkludera denna kategori)
        await EnsureUniqueNameAsync(newName, excludeId: entity.Id, ct);

        current.Name = newName;

        await _uow.ToolCategories.UpdateAsync(current, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // SOFT DELETE: markera som borttagen (global query filter döljer den)
        var existing = await _uow.ToolCategories.GetByIdAsync(id, ct);
        if (existing is null) return false;

        existing.DeletedAtUtc = DateTime.UtcNow;
        await _uow.ToolCategories.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => _uow.ToolCategories.NameExistsAsync(name.Trim(), excludeId, ct);

    // -----------------------
    // Helpers
    // -----------------------
    private async Task EnsureUniqueNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        if (await _uow.ToolCategories.NameExistsAsync(name, excludeId, ct))
            throw new InvalidOperationException("A category with this name already exists.");
    }
}
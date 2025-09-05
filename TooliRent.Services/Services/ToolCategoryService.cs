using AutoMapper;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.ToolCategories;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class ToolCategoryService : IToolCategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ToolCategoryService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ToolCategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _uow.ToolCategories.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<ToolCategoryDto>>(items);
    }

    public async Task<ToolCategoryDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.ToolCategories.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<ToolCategoryDto>(entity);
    }

    public async Task<ToolCategoryDto> CreateAsync(ToolCategoryCreateDto dto, CancellationToken ct = default)
    {
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(dto));

        // case-insensitive exists
        if (await _uow.ToolCategories.NameExistsAsync(name, excludeId: null, ct))
            throw new InvalidOperationException($"Category '{name}' already exists.");

        var entity = _mapper.Map<ToolCategory>(dto);
        entity.Name = name;

        await _uow.ToolCategories.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<ToolCategoryDto>(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, ToolCategoryUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.ToolCategories.GetByIdAsync(id, ct);
        if (existing is null) return false;

        var newName = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name is required.", nameof(dto));

        if (await _uow.ToolCategories.NameExistsAsync(newName, excludeId: id, ct))
            throw new InvalidOperationException($"Category '{newName}' already exists.");

        existing.Name = newName;

        await _uow.ToolCategories.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _uow.ToolCategories.GetByIdAsync(id, ct);
        if (existing is null) return false;

        // Soft delete
        existing.DeletedAtUtc = DateTime.UtcNow;
        await _uow.ToolCategories.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => _uow.ToolCategories.NameExistsAsync((name ?? string.Empty).Trim(), excludeId, ct);
}
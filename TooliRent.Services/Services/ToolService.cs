using AutoMapper;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Tools;

namespace TooliRent.Services.Services;

public class ToolService : IToolService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ToolService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<ToolDto> Items, int Total)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page,
        int pageSize,
        string? categoryName = null,
        CancellationToken ct = default)
    {
        // Om categoryId saknas men categoryName angivits: slå upp id via namn
        if (!categoryId.HasValue && !string.IsNullOrWhiteSpace(categoryName))
        {
            var cat = await _uow.ToolCategories.GetByNameAsync(categoryName, ct);
            if (cat is null)
                return (Enumerable.Empty<ToolDto>(), 0);

            categoryId = cat.Id;
        }

        var (items, total) = await _uow.Tools.SearchAsync(categoryId, isAvailable, query, page, pageSize, ct);
        var mapped = _mapper.Map<IEnumerable<ToolDto>>(items);
        return (mapped, total);
    }

    public async Task<ToolDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Tools.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<ToolDto>(entity);
    }

    public async Task<ToolDto> CreateAsync(ToolCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required.", nameof(dto));

        var entity = _mapper.Map<Tool>(dto);
        await _uow.Tools.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.Tools.GetByIdAsync(entity.Id, ct);
        return _mapper.Map<ToolDto>(created!);
    }

    public async Task<bool> UpdateAsync(Guid id, ToolUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.Tools.GetByIdAsync(id, ct);
        if (existing is null) return false;

        _mapper.Map(dto, existing);
        existing.Id = id;

        await _uow.Tools.UpdateAsync(existing, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Tools.GetByIdAsync(id, ct);
        if (entity is null) return false;

        entity.DeletedAtUtc = DateTime.UtcNow; // soft delete
        await _uow.Tools.UpdateAsync(entity, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
    
    public async Task<IEnumerable<ToolDto>> GetAvailableInWindowAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var tools = await _uow.Tools.GetAvailableInWindowAsync(fromUtc, toUtc, ct);
        return _mapper.Map<IEnumerable<ToolDto>>(tools);
    }
}
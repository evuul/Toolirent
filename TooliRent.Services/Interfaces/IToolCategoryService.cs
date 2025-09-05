using TooliRent.Services.DTOs.ToolCategories;

namespace TooliRent.Services.Interfaces;

public interface IToolCategoryService
{
    Task<IEnumerable<ToolCategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<ToolCategoryDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ToolCategoryDto> CreateAsync(ToolCategoryCreateDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, ToolCategoryUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
using TooliRent.Services.DTOs.Tools;

public interface IToolService
{
    Task<(IEnumerable<ToolDto> Items, int Total)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page,
        int pageSize,
        string? categoryName = null,   // <— nytt
        CancellationToken ct = default);

    Task<ToolDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ToolDto> CreateAsync(ToolCreateDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, ToolUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
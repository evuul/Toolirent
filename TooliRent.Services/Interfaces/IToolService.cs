using TooliRent.Core.Models;

namespace TooliRent.Services.Interfaces;

public interface IToolService
{
    Task<(IEnumerable<Tool> Items, int Total)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page, 
        int pageSize,
        CancellationToken ct = default);

    Task<Tool?> GetAsync(Guid id, CancellationToken ct = default);

    Task<Tool> CreateAsync(Tool tool, CancellationToken ct = default);

    Task<bool> UpdateAsync(Tool tool, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
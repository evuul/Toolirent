using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IToolRepository : IRepository<Tool>
{
    Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<(IEnumerable<Tool> Items, int TotalCount)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,   // <— namnjustering
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);
}
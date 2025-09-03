using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IToolRepository : IRepository<Tool>
{
    Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    /// <summary>
    /// Sök med valfria filter. Paging för list-endpoints.
    /// </summary>
    Task<(IEnumerable<Tool> Items, int TotalCount)> SearchAsync(
        Guid? categoryId,
        bool? isActive,
        string? query,      // fritext på Name/Description
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Verktyg som inte är utlånade nu och inte reserverade inom intervallet.
    /// </summary>
    Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);
}
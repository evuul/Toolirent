using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IToolRepository : IRepository<Tool>
{
    Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<(IEnumerable<Tool> Items, int TotalCount)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // Kollar vilka verktyg som är lediga i ett givet tidsfönster
    Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);

    // Kollar specifikt för ett verktyg om det är ledigt i ett givet tidsfönster
    Task<bool> IsAvailableInWindowAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);
    
    Task<bool> IsAvailableInWindowIgnoringAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? ignoreReservationId = null,
        Guid? ignoreLoanId = null,
        CancellationToken ct = default);
}
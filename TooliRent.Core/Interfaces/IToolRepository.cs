using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IToolRepository : IRepository<Tool>
{
    // ---------- Browsing/Listing ----------
    Task<IEnumerable<Tool>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<(IEnumerable<Tool> Items, int TotalCount)> SearchAsync(
        Guid? categoryId,
        bool? isAvailable,
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // ---------- Availability (READ) ----------

    /// <summary>
    /// Kollar vilka verktyg som är lediga i ett givet tidsfönster.
    /// (Behålls för bakåtkompatibilitet. För batch mot specifika toolIds, använd AreAvailableInWindowAsync.)
    /// </summary>
    Task<IEnumerable<Tool>> GetAvailableInWindowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Kollar om ett specifikt verktyg är ledigt i ett givet tidsfönster.
    /// </summary>
    Task<bool> IsAvailableInWindowAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);

    /// <summary>
    /// BATCH: Returnerar en bool per ToolId om det är ledigt i tidsfönstret.
    /// Du kan ignorera en befintlig Reservation eller ett Loan (vid t.ex. ombokning/checkout från reservation).
    /// </summary>
    Task<IDictionary<Guid, bool>> AreAvailableInWindowAsync(
        IEnumerable<Guid> toolIds,
        DateTime startUtc,
        DateTime endUtc,
        Guid? ignoreReservationId = null,
        Guid? ignoreLoanId = null,
        CancellationToken ct = default);

    /// <summary>
    /// (Legacy) Använd AreAvailableInWindowAsync med ignoreReservationId/ignoreLoanId istället.
    /// </summary>
    [Obsolete("Use AreAvailableInWindowAsync(toolIds, startUtc, endUtc, ignoreReservationId, ignoreLoanId, ct).")]
    Task<bool> IsAvailableInWindowIgnoringAsync(
        Guid toolId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? ignoreReservationId = null,
        Guid? ignoreLoanId = null,
        CancellationToken ct = default);
}
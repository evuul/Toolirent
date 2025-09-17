// TooliRent.Core/Interfaces/IReservationRepository.cs
using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<Reservation>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Finns överlappande aktiv reservation för verktyget i intervallet?</summary>
    Task<bool> HasOverlapAsync(Guid toolId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>Avboka (status -> Cancelled). Historik bevaras.</summary>
    Task MarkCancelledAsync(Reservation reservation, CancellationToken ct = default);
}
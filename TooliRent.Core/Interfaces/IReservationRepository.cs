using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<bool> HasOverlapAsync(Guid toolId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    // Avboka: spara bara statusändring (ingen hårdradering vi behåller historik).
    Task MarkCancelledAsync(Reservation reservation, CancellationToken ct = default);
}
using TooliRent.Core.Models;

namespace TooliRent.Services.Interfaces;

public interface IReservationService
{
    Task<Reservation?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Reservation>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);

    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default);
    Task<bool> CancelAsync(Guid id, CancellationToken ct = default);
    Task<bool> CompleteAsync(Guid id, Guid loanId, CancellationToken ct = default); // markera som avslutad kopplad till loan
}
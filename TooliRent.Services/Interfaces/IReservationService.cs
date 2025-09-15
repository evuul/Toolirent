using TooliRent.Services.DTOs.Reservations;

namespace TooliRent.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ReservationDto>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<ReservationDto> CreateAsync(ReservationCreateDto dto, CancellationToken ct = default);
    Task<ReservationBatchResultDto> CreateBatchAsync(ReservationBatchCreateDto dto, CancellationToken ct = default);

    Task<bool> CancelAsync(Guid id, CancellationToken ct = default);
    Task<bool> CompleteAsync(Guid id, Guid loanId, CancellationToken ct = default);
}
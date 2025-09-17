// TooliRent.Services/Interfaces/IReservationService.cs
using TooliRent.Services.DTOs.Reservations;

namespace TooliRent.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(ReservationCreateDto dto, CancellationToken ct);
    Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ReservationDto?> GetForMemberAsync(Guid id, Guid memberId, CancellationToken ct);
    Task<bool> CancelAsync(Guid id, Guid? actingMemberId, CancellationToken ct); // actingMemberId != null => ägarkontroll
    Task<IReadOnlyList<ReservationDto>> GetActiveForMemberAsync(Guid memberId, CancellationToken ct);
    Task<IReadOnlyList<ReservationDto>> GetHistoryForMemberAsync(Guid memberId, int skip, int take, CancellationToken ct);
    Task<ReservationBatchResultDto> CreateBatchAsync(
        ReservationBatchCreateDto dto,
        CancellationToken ct = default);
}
using TooliRent.Services.DTOs.Reservations;

namespace TooliRent.Services.Interfaces;

public interface IReservationQueries
{
    /// Detalj: inkluderar Items.
    Task<ReservationDto?> GetDtoByIdAsync(Guid id, CancellationToken ct = default);

    /// Aktiva för medlem: lättvikt (Items tom), närmast först.
    Task<IReadOnlyList<ReservationDto>> GetActiveDtosForMemberAsync(Guid memberId, CancellationToken ct = default);

    /// Historik för medlem: lättvikt (Items tom), paginerat.
    Task<IReadOnlyList<ReservationDto>> GetHistoryDtosForMemberAsync(Guid memberId, int skip, int take, CancellationToken ct = default);

    /// Admin-sök (valfritt, om du har en admin-vy med paging + total)
    Task<(IReadOnlyList<ReservationDto> Items, int Total)> AdminSearchDtosAsync(
        DateTime? fromUtc, DateTime? toUtc, string? status, int page, int pageSize, CancellationToken ct = default);
}
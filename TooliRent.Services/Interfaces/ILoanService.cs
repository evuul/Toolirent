// TooliRent.Services/Interfaces/ILoanService.cs
using TooliRent.Services.DTOs.Loans;

namespace TooliRent.Services.Interfaces;

public interface ILoanService
{
    // Hämta ett enskilt lån
    Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default);

    Task<LoanDto?> ReturnAsMemberAsync(Guid id, Guid currentMemberId, LoanReturnDto dto, CancellationToken ct = default);
    Task<LoanDto?> ReturnAsAdminAsync(Guid id, AdminLoanReturnDto dto, CancellationToken ct = default);

    // Sök/paginera lån (admin eller intern användning)
    Task<(IEnumerable<LoanDto> Items, int Total)> SearchAsync(
        Guid? memberId,
        Guid? toolId,
        int? status,
        bool openOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // =========================
    // NYA BATCH-METODER
    // =========================

    // Medlem: kan checka ut 1..N verktyg i ett anrop.
    // - MemberId tas ALLTID från JWT och skickas in som currentMemberId.
    // - Varje item får antingen ReservationId (enkelt) ELLER ToolId + DueAtUtc (direktlån).
    Task<IEnumerable<LoanDto>> CheckoutBatchForMemberAsync(
        IEnumerable<LoanCheckoutDto> items,
        Guid currentMemberId,
        CancellationToken ct = default);

    // Admin: kan checka ut 1..N verktyg åt valfri medlem.
    // - MemberId är ett fält i varje item.
    Task<IEnumerable<LoanDto>> CheckoutBatchForAdminAsync(
        IEnumerable<AdminLoanCheckoutDto> items,
        CancellationToken ct = default);
}
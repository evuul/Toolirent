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
    // GEMENSAM BATCH-METOD
    // =========================

    /// <summary>
    /// Skapar ett eller flera lån i en batch.
    /// - Me-endpoints: currentMemberId hämtas från JWT.
    /// - Admin-endpoints: currentMemberId kommer från routen (admin väljer medlem).
    /// - Varje item får antingen ReservationId (enkelt) eller ToolIds + DueAtUtc (direktlån).
    /// </summary>
    Task<IEnumerable<LoanDto>> CheckoutBatchForMemberAsync(
        IEnumerable<LoanCheckoutDto> items,
        Guid currentMemberId,
        CancellationToken ct = default);
}
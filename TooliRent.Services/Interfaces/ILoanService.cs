using TooliRent.Core.Enums;
using TooliRent.Core.Models;

namespace TooliRent.Services.Interfaces;

public interface ILoanService
{
    Task<Loan?> GetAsync(Guid id, CancellationToken ct = default);

    Task<(IEnumerable<Loan> Items, int Total)> SearchAsync(
        Guid? memberId,
        Guid? toolId,
        LoanStatus? status,
        bool openOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Loan> CheckoutAsync(Guid toolId, Guid memberId, DateTime dueAtUtc, Guid? reservationId = null, CancellationToken ct = default);

    Task<bool> ReturnAsync(Guid loanId, DateTime returnedAtUtc, CancellationToken ct = default);

    Task<IEnumerable<Loan>> GetOverdueAsync(DateTime asOfUtc, CancellationToken ct = default);

    Task<IEnumerable<Loan>> GetOpenByMemberAsync(Guid memberId, CancellationToken ct = default);
}
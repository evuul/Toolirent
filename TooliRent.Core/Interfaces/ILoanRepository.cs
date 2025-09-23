using TooliRent.Core.Enums;
using TooliRent.Core.Models;
using TooliRent.Core.Models.Admin;

namespace TooliRent.Core.Interfaces;

public interface ILoanRepository : IRepository<Loan>
{
    Task<IEnumerable<Loan>> GetOpenByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<bool> ToolIsLoanedNowAsync(Guid toolId, CancellationToken ct = default);
    Task<IEnumerable<Loan>> GetOverdueAsync(DateTime asOfUtc, CancellationToken ct = default);

    /// <summary>
    /// Lista/filtrera lån för adminvy.
    /// </summary>
    Task<(IEnumerable<Loan> Items, int TotalCount)> SearchAsync(
        Guid? memberId,
        Guid? toolId,
        LoanStatus? status,
        bool openOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<AdminStatsResult> GetAdminStatsAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}
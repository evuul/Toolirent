using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class LoanService : ILoanService
{
    private readonly IUnitOfWork _uow;

    public LoanService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<Loan?> GetAsync(Guid id, CancellationToken ct = default)
        => _uow.Loans.GetByIdAsync(id, ct);

    public Task<(IEnumerable<Loan> Items, int Total)> SearchAsync(
        Guid? memberId, Guid? toolId, LoanStatus? status,
        bool openOnly, int page, int pageSize, CancellationToken ct = default)
        => _uow.Loans.SearchAsync(memberId, toolId, status, openOnly, page, pageSize, ct);

    public async Task<Loan> CheckoutAsync(Guid toolId, Guid memberId, DateTime dueAtUtc, Guid? reservationId = null, CancellationToken ct = default)
    {
        // kontrollera om verktyget är ledigt
        if (await _uow.Loans.ToolIsLoanedNowAsync(toolId, ct))
            throw new InvalidOperationException("Tool is already loaned out.");

        var loan = new Loan
        {
            ToolId = toolId,
            MemberId = memberId,
            CheckedOutAtUtc = DateTime.UtcNow,
            DueAtUtc = dueAtUtc,
            ReservationId = reservationId,
            Status = LoanStatus.Open
        };

        await _uow.Loans.AddAsync(loan, ct);

        // uppdatera tool-status
        var tool = await _uow.Tools.GetByIdAsync(toolId, ct);
        if (tool is not null)
        {
            tool.IsAvailable = false;
            await _uow.Tools.UpdateAsync(tool, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return loan;
    }

    public async Task<bool> ReturnAsync(Guid loanId, DateTime returnedAtUtc, CancellationToken ct = default)
    {
        var loan = await _uow.Loans.GetByIdAsync(loanId, ct);
        if (loan is null) return false;

        loan.ReturnedAtUtc = returnedAtUtc;
        loan.Status = loan.DueAtUtc < returnedAtUtc ? LoanStatus.Late : LoanStatus.Returned;

        await _uow.Loans.UpdateAsync(loan, ct);

        // sätt tool som tillgängligt igen
        var tool = await _uow.Tools.GetByIdAsync(loan.ToolId, ct);
        if (tool is not null)
        {
            tool.IsAvailable = true;
            await _uow.Tools.UpdateAsync(tool, ct);
        }

        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public Task<IEnumerable<Loan>> GetOverdueAsync(DateTime asOfUtc, CancellationToken ct = default)
        => _uow.Loans.GetOverdueAsync(asOfUtc, ct);

    public Task<IEnumerable<Loan>> GetOpenByMemberAsync(Guid memberId, CancellationToken ct = default)
        => _uow.Loans.GetOpenByMemberAsync(memberId, ct);
}
using TooliRent.Services.DTOs.Loans;

namespace TooliRent.Services.Interfaces;

public interface ILoanService
{
    Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<LoanDto> CheckoutAsync(LoanCheckoutDto dto, CancellationToken ct = default);
    Task<LoanDto?> ReturnAsync(LoanReturnDto dto, CancellationToken ct = default);
}
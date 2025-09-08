using TooliRent.Services.DTOs.Loans;

public interface ILoanService
{
    Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<LoanDto>  CheckoutAsync(LoanCheckoutDto dto, CancellationToken ct = default);

    // Return ska ge tillbaka det uppdaterade lånet (inte bool)
    Task<LoanDto?> ReturnAsync(Guid id, LoanReturnDto dto, CancellationToken ct = default);

    // Admin-sök: status som int? (0=Open,1=Returned,2=Late); mappas till enum i service
    Task<(IEnumerable<LoanDto> Items, int Total)> SearchAsync(
        Guid? memberId,
        Guid? toolId,
        int? status,
        bool openOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
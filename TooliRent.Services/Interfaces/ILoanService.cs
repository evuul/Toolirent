using TooliRent.Services.DTOs.Loans;

public interface ILoanService
{
    Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default);

    // Direktutlåning (utan reservation)
    Task<LoanDto> CheckoutAsync(LoanCheckoutDto dto, CancellationToken ct = default);

    // Utlåning från reservation
    Task<LoanDto> CheckoutFromReservationAsync(LoanCheckoutFromReservationDto dto, CancellationToken ct = default);

    // Returnera lån – returnerar det uppdaterade lånet
    Task<LoanDto?> ReturnAsync(Guid id, LoanReturnDto dto, CancellationToken ct = default);

    // Admin-sök: status som int? (0=Open,1=Returned,2=Late) – mappas internt till enum
    Task<(IEnumerable<LoanDto> Items, int Total)> SearchAsync(
        Guid? memberId,
        Guid? toolId,
        int? status,
        bool openOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
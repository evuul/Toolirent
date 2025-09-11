namespace TooliRent.Services.DTOs.Loans;

// För direktutlåning av verktyg utan reservation
public record LoanCheckoutDto(
    Guid ToolId,
    Guid MemberId,
    DateTime DueAtUtc
);
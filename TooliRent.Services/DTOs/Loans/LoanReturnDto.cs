namespace TooliRent.Services.DTOs.Loans;

public record LoanReturnDto(
    Guid LoanId,
    DateTime ReturnedAtUtc,
    string? Notes
);
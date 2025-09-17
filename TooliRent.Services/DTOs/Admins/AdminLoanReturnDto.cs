namespace TooliRent.Services.DTOs.Loans;

/// <summary>
/// Används av admin för att återlämna ett lån.
/// - Admin kan sätta ReturnedAtUtc manuellt
/// - Kan även lägga till Notes
/// </summary>
public record AdminLoanReturnDto(
    Guid LoanId,
    DateTime ReturnedAtUtc,
    string? Notes
);
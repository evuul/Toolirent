namespace TooliRent.Services.DTOs.Loans;

/// <summary>
/// Används när en medlem själv återlämnar sitt lån.
/// - MemberId tas från JWT-claim
/// - ReturnedAtUtc sätts alltid av servern
/// </summary>
public record LoanReturnDto(
    string? Notes
);
namespace TooliRent.Services.DTOs.Loans;

public record LoanDto(
    Guid Id,
    Guid ToolId,
    string ToolName,
    Guid MemberId,
    string MemberName,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    int Status,        // castas från enum
    decimal? LateFee,
    string? Notes
);
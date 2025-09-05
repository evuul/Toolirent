namespace TooliRent.Services.DTOs.Loans;

public record LoanCheckoutDto(
    Guid ToolId,
    Guid MemberId,
    DateTime DueAtUtc,
    Guid? ReservationId
);

public record LoanReturnDto(DateTime ReturnedAtUtc);

public record LoanDto(
    Guid Id,
    Guid ToolId,
    string ToolName,
    Guid MemberId,
    string MemberName,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    int Status,
    decimal? LateFee
);
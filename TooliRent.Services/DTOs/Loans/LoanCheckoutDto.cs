namespace TooliRent.Services.DTOs.Loans;

public record LoanCheckoutDto(
    Guid? ReservationId,
    Guid ToolId,
    Guid MemberId,
    DateTime DueAtUtc
);
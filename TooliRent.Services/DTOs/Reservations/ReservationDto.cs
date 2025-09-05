namespace TooliRent.Services.DTOs.Reservations;

public record ReservationDto(
    Guid Id,
    Guid ToolId,
    string ToolName,
    Guid MemberId,
    string MemberName,
    DateTime StartUtc,
    DateTime EndUtc,
    decimal TotalPrice,
    bool IsPaid,
    int Status // castas från enum i mappning
);
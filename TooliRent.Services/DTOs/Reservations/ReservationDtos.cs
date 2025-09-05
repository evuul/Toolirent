namespace TooliRent.Services.DTOs.Reservations;

public record ReservationCreateDto(
    Guid ToolId,
    Guid MemberId,
    DateTime StartUtc,
    DateTime EndUtc
);

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
    int Status // mappar enum som int/ev. string vid behov
);
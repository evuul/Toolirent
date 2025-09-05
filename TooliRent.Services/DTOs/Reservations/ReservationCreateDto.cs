namespace TooliRent.Services.DTOs.Reservations;

public record ReservationCreateDto(
    Guid ToolId,
    Guid MemberId,
    DateTime StartUtc,
    DateTime EndUtc
);
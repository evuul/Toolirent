namespace TooliRent.Services.DTOs.Reservations;

public record ReservationCreateDto(
    IEnumerable<Guid> ToolIds,
    DateTime StartUtc,
    DateTime EndUtc,
    Guid? MemberId = null // ignoreras i "Me" endpoints, krävs i Admin
);
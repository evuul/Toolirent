namespace TooliRent.Services.DTOs.Reservations;

// Samlingsresultat av batchen
public record ReservationBatchResultDto(
    Guid MemberId,
    DateTime StartUtc,
    DateTime EndUtc,
    IEnumerable<ReservationDto> Reservations
);
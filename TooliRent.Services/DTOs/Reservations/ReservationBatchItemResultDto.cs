namespace TooliRent.Services.DTOs.Reservations;

// Resultat per verktyg
public record ReservationBatchItemResultDto(
    Guid ToolId,
    bool Success,
    string? Error,
    ReservationDto? Reservation
);
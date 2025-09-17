namespace TooliRent.Services.DTOs.Reservations;

public record ReservationUpdateDto(
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsPaid,
    int Status
);
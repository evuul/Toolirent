namespace TooliRent.Services.DTOs.Reservations;

// Används av *Admin* för att boka åt en specifik medlem.
public record ReservationAdminCreateDto(
    Guid MemberId,
    IEnumerable<Guid> ToolIds,
    DateTime StartUtc,
    DateTime EndUtc
);
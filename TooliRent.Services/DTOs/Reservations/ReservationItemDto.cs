namespace TooliRent.Services.DTOs.Reservations;

public record ReservationItemDto(
    Guid ToolId, 
    string ToolName, 
    decimal PricePerDay
    );
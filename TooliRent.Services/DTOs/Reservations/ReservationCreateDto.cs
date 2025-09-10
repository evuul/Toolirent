namespace TooliRent.Services.DTOs.Reservations;

public record ReservationCreateDto
{
    public Guid ToolId { get; init; }
    public Guid MemberId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
}
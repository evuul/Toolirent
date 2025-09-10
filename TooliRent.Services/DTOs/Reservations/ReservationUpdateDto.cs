namespace TooliRent.Services.DTOs.Reservations;

public record ReservationUpdateDto
{
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public bool IsPaid { get; init; }
    public int Status { get; init; }
}
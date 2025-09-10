namespace TooliRent.Services.DTOs.Reservations;

public record ReservationDto
{
    public Guid Id { get; init; }
    public Guid ToolId { get; init; }
    public string ToolName { get; init; } = string.Empty;
    public Guid MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public decimal TotalPrice { get; init; }
    public bool IsPaid { get; init; }
    public int Status { get; init; } // enum -> int
}
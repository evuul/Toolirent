namespace TooliRent.Core.Models;

public class ReservationItem : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    // Pris fryses vid bokning (robust mot framtida prisändringar)
    public decimal PricePerDay { get; set; }
}
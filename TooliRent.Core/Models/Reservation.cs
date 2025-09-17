using TooliRent.Core.Enums;

namespace TooliRent.Core.Models;
public class Reservation : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc   { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    // NYTT: ersätter single ToolId/Tool
    public ICollection<ReservationItem> Items { get; set; } = new List<ReservationItem>();

    // Koppling till lån som skapats från denna reservation (kan vara 1-many i framtiden,
    // men vi börjar 1-1 för "allt eller inget"-checkout)
    public Loan? Loan { get; set; }
}
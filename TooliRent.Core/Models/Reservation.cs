using TooliRent.Core.Enums;

namespace TooliRent.Core.Models;
public class Reservation : BaseEntity
{
    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc   { get; set; }

    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
    
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    // Om bokningen resulterade i en faktisk utlåning
    public Guid? LoanId { get; set; }
    public Loan? Loan { get; set; }
}
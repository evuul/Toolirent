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

    // valfri koppling till faktisk utlåning om den blev av
    public Guid? LoanId { get; set; }
    public Loan? Loan { get; set; }
}
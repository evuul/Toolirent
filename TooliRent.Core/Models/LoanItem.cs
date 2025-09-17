namespace TooliRent.Core.Models;

public class LoanItem : BaseEntity
{
    public Guid LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    public decimal PricePerDay { get; set; }
}
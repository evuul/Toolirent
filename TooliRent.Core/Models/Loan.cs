using TooliRent.Core.Enums;

namespace TooliRent.Core.Models;

public class Loan : BaseEntity
{
    // BEHÅLL: medlem, tider, status, avgifter, koppling bak
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DateTime CheckedOutAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Open;

    public Guid? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public decimal? LateFee { get; set; }
    public string? Notes { get; set; }
    public string? ProcessedByUserId { get; set; }

    // NYTT: ersätter single ToolId/Tool
    public ICollection<LoanItem> Items { get; set; } = new List<LoanItem>();

    // (valfritt) denormaliserat totalpris för snabb listning
    public decimal TotalPrice { get; set; }
}
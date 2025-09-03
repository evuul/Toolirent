using TooliRent.Core.Enums;

namespace TooliRent.Core.Models;

public class Loan : BaseEntity
{
    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DateTime CheckedOutAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Open; // Open/Returned/Late

    // (valfritt) koppling bak till reservationen som låg till grund
    public Guid? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    // avgifter/anteckningar
    public decimal? LateFee { get; set; }
    public string? Notes { get; set; }

    // (om ni har disk/personal som hanterar utlämningen)
    public string? ProcessedByUserId { get; set; }
}
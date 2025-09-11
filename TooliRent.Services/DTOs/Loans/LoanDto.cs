namespace TooliRent.Services.DTOs.Loans;

public class LoanDto
{
    public Guid Id { get; set; }

    public Guid ToolId { get; set; }
    public string ToolName { get; set; } = "";

    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = "";

    public Guid? ReservationId { get; set; }

    public DateTime CheckedOutAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    public int Status { get; set; }           // eller string om du hellre vill
    public decimal? LateFee { get; set; }
    public string? Notes { get; set; }
}
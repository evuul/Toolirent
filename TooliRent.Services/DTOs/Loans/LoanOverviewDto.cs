namespace TooliRent.Services.DTOs.Loans;

public class LoanOverviewDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public DateTime CheckedOutAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public int Status { get; set; }
}
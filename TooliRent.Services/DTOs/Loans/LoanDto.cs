// TooliRent.Services/DTOs/Loans/LoanDto.cs
namespace TooliRent.Services.DTOs.Loans;

public class LoanDto
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;

    public DateTime CheckedOutAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    public int Status { get; set; }

    public int ItemCount { get; set; }
    public string FirstToolName { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    public IEnumerable<LoanItemDto> Items { get; set; } = Array.Empty<LoanItemDto>();
}
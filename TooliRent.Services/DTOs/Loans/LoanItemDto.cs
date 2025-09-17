namespace TooliRent.Services.DTOs.Loans;

/// <summary>
/// Representerar ett enskilt verktyg i ett lån.
/// </summary>
public record LoanItemDto(
    Guid ToolId,
    string ToolName,
    decimal PricePerDay
);
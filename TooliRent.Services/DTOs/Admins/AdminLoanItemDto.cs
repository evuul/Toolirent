namespace TooliRent.Services.DTOs.Admins;

public record AdminLoanItemDto(
    Guid Id,
    Guid ToolId,
    string ToolName,
    Guid MemberId,
    string MemberName,
    DateTime CheckedOutAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    decimal? LateFee,
    string Status);
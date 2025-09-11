namespace TooliRent.Services.DTOs.Admins;

public record AdminReservationItemDto(
    Guid Id,
    Guid ToolId,
    string ToolName,
    Guid MemberId,
    string MemberName,
    DateTime StartUtc,
    DateTime EndUtc,
    decimal TotalPrice,
    bool IsPaid,
    string Status);
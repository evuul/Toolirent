namespace TooliRent.Services.DTOs.Admins;

public record AdminStatsDto(
    int ToolsTotal,
    int LoansTotal,
    int LoansOpen,
    int LoansReturned,
    int LoansLate,
    int ReservationsTotal,
    int ReservationsActive,
    decimal RevenueTotal,          // summerad TotalPrice (res/lån) i intervallet
    IReadOnlyList<TopToolDto> TopToolsByLoans, // topp 5
    IReadOnlyList<CategoryUtilizationDto> CategoryUtilization, // utnyttjande per kategori
    IReadOnlyList<MemberActivityDto> TopMembersByLoans // topp 5
);

public record TopToolDto(Guid ToolId, string ToolName, int LoansCount);
public record CategoryUtilizationDto(Guid CategoryId, string CategoryName, double UtilizationPct);
public record MemberActivityDto(Guid MemberId, string MemberName, int LoansCount);
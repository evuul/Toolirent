namespace TooliRent.Core.Models.Admin;

public class AdminStatsResult
{
    public int ToolsTotal { get; set; }
    public int LoansTotal { get; set; }
    public int LoansOpen { get; set; }
    public int LoansReturned { get; set; }
    public int LoansLate { get; set; }
    public int ReservationsTotal { get; set; }
    public int ReservationsActive { get; set; }
    public decimal RevenueTotal { get; set; }

    public List<TopToolItem> TopToolsByLoans { get; set; } = new();
    public List<CategoryUtilizationItem> CategoryUtilization { get; set; } = new();
    public List<MemberActivityItem> TopMembersByLoans { get; set; } = new();
}
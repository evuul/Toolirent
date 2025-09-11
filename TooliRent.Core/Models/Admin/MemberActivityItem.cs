namespace TooliRent.Core.Models.Admin;

public class MemberActivityItem
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public int LoansCount { get; set; }
}
namespace TooliRent.Core.Models.Admin;

public class TopToolItem
{
    public Guid ToolId { get; set; }
    public string ToolName { get; set; } = "";
    public int LoansCount { get; set; }
}
namespace TooliRent.Core.Models.Admin;

public class CategoryUtilizationItem
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public double UtilizationPct { get; set; }
}
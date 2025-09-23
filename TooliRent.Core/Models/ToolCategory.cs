namespace TooliRent.Core.Models;

public class ToolCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Tool> Tools { get; set; } = new List<Tool>();
}
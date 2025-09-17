namespace TooliRent.Services.DTOs.ToolCategories;

public record ToolCategoryDto 
(
    Guid Id,
    string Name,
    int TotalTools,
    int AvailableTools
    );
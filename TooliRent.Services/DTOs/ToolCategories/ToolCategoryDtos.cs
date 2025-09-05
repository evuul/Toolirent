namespace TooliRent.Services.DTOs.ToolCategories;

public record ToolCategoryCreateDto(string Name);
public record ToolCategoryUpdateDto(string Name);
public record ToolCategoryDto(Guid Id, string Name);
// TooliRent.Services/DTOs/Tools/ToolDtos.cs
namespace TooliRent.Services.DTOs.Tools;

public record ToolCreateDto(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal RentalPricePerDay
);

public record ToolUpdateDto(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal RentalPricePerDay,
    bool IsAvailable
);

public record ToolDto(
    Guid Id,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    decimal RentalPricePerDay,
    bool IsAvailable
);
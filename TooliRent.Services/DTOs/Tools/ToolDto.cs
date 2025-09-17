namespace TooliRent.Services.DTOs.Tools;

public sealed record ToolDto(
    Guid Id,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    decimal RentalPricePerDay,
    bool IsAvailable
);
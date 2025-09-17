namespace TooliRent.Services.DTOs.Tools;

public sealed record ToolCreateDto(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal RentalPricePerDay
);
namespace TooliRent.Services.DTOs.Tools;

public sealed record ToolUpdateDto(
    string Name,
    string? Description,
    Guid CategoryId,
    decimal RentalPricePerDay,
    bool IsAvailable
);
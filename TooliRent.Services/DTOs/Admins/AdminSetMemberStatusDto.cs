namespace TooliRent.Services.DTOs.Admins;

/// <summary>
/// Enkel DTO för att aktivera/inaktivera en medlem.
/// </summary>
public record AdminSetMemberStatusDto
{
    public bool IsActive { get; init; }
}
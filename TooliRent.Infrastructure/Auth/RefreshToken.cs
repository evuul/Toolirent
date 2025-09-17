using System.ComponentModel.DataAnnotations;

namespace TooliRent.Infrastructure.Auth.Models;

public class RefreshToken
{
    [Key] public Guid Id { get; set; }
    [Required] public string UserId { get; set; } = null!;   // IdentityUser.Id
    [Required] public string TokenHash { get; set; } = null!; // lagra hash, inte raw
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
}
namespace TooliRent.Services.DTOs.Auths;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid? MemberId
);
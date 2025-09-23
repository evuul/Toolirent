namespace TooliRent.Services.DTOs.Auths;

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    Guid? MemberId
);
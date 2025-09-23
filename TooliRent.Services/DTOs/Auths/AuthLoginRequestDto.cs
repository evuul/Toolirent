namespace TooliRent.Services.DTOs.Auths;

public record AuthLoginRequestDto(
    string Email,
    string Password
);
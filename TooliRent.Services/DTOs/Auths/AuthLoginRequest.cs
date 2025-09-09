namespace TooliRent.Services.DTOs.Auths;

public record AuthLoginRequest(
    string Email,
    string Password
);
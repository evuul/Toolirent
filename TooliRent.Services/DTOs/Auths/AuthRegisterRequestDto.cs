namespace TooliRent.Services.DTOs.Auths;

public record AuthRegisterRequestDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
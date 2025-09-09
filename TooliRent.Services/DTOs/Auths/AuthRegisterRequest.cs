namespace TooliRent.Services.DTOs.Auths;

public record AuthRegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
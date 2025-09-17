namespace TooliRent.Services.DTOs.Members;

public record MemberCreateDto(
    string FirstName,
    string LastName,
    string Email
);
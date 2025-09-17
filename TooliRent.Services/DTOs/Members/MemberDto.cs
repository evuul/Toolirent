namespace TooliRent.Services.DTOs.Members;

public record MemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email
);
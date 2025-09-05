namespace TooliRent.Services.DTOs.Members;

public record MemberUpdateDto(
    string FirstName,
    string LastName,
    string Email
);
namespace TooliRent.Services.DTOs.Members;

public record MemberCreateDto(string FirstName, string LastName, string Email);
public record MemberUpdateDto(string FirstName, string LastName, string Email);

public record MemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email
);
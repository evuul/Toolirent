namespace TooliRent.Services.DTOs.Members;

public record MemberDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName  { get; init; } = string.Empty;
    public string Email     { get; init; } = string.Empty;

    // NYTT: behövs för ActiveMemberHandler
    public bool IsActive    { get; init; }
    public int  TokenVersion { get; init; }
}
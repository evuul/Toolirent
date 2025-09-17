using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetMemberId(this ClaimsPrincipal user, out Guid memberId)
    {
        memberId = Guid.Empty;
        var raw = user.FindFirstValue("memberId") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out memberId);
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TooliRent.Infrastructure.Data;
using TooliRent.Services.Interfaces;
using TooliRent.WebAPI.Validators.Auth; // TooliRentDbContext

namespace TooliRent.Services.Validators.Auths;

/// <summary>
/// Säkerställer att en "Member":
/// 1) finns,
/// 2) är aktiv,
/// 3) att token "ver"-claim matchar Member.TokenVersion i DB.
/// </summary>
public sealed class ActiveMemberHandler : AuthorizationHandler<ActiveMemberRequirement>
{
    private readonly IMemberService _members;

    public ActiveMemberHandler(IMemberService members)
    {
        _members = members;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ActiveMemberRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        // Policyn är för endpoints där vi kräver Role=Member
        var isMember = user.Claims.Any(c => c.Type == ClaimTypes.Role &&
                                            string.Equals(c.Value, "Member", StringComparison.OrdinalIgnoreCase));
        if (!isMember) return;

        var memberIdStr = user.FindFirst("memberId")?.Value;
        var verStr      = user.FindFirst("ver")?.Value;
        if (!Guid.TryParse(memberIdStr, out var memberId)) return;
        if (!int.TryParse(verStr, out var tokenVersion)) return;

        // Läs medlem via service
        var dto = await _members.GetAsync(memberId);
        if (dto is null) return;

        if (dto.IsActive && dto.TokenVersion == tokenVersion)
            context.Succeed(requirement);
    }
}
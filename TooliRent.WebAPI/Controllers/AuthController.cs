using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using TooliRent.Infrastructure.Data;          // Domain DB (Members)
using TooliRent.Infrastructure.Auth;         // AuthDbContext (Identity + RefreshTokens)
using TooliRent.Infrastructure.Auth.Models;  // RefreshToken
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Auths;         // AuthLoginRequestDto, AuthRegisterRequestDto, AuthRefreshRequestDto

namespace TooliRent.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userMgr;
    private readonly IConfiguration _cfg;
    private readonly TooliRentDbContext _domainDb;
    private readonly AuthDbContext _authDb;

    // Centrala livtider (styr via appsettings: "Auth": { "AccessTokenMinutes": 15, "RefreshTokenDays": 30 })
    private TimeSpan AccessTokenLifetime =>
        TimeSpan.FromMinutes(_cfg.GetValue<int?>("Auth:AccessTokenMinutes") ?? 15);

    private TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(_cfg.GetValue<int?>("Auth:RefreshTokenDays") ?? 30);

    public AuthController(
        UserManager<IdentityUser> userMgr,
        IConfiguration cfg,
        TooliRentDbContext domainDb,
        AuthDbContext authDb)
    {
        _userMgr = userMgr;
        _cfg = cfg;
        _domainDb = domainDb;
        _authDb = authDb;
    }

    // -------- LOGIN --------
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequestDto dto, CancellationToken ct)
    {
        var user = await _userMgr.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized("Invalid login attempt.");

        var ok = await _userMgr.CheckPasswordAsync(user, dto.Password);
        if (!ok) return Unauthorized("Invalid login attempt.");

        var member = await _domainDb.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdentityUserId == user.Id, ct);

        var (jwt, accessExpires) = await GenerateJwtAsync(user, member?.Id);
        var (refreshToken, refreshExpires) = await IssueRefreshTokenAsync(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new
        {
            token = jwt,
            expires = accessExpires,    // JWT expiry (UTC)
            refreshToken,
            refreshExpires              // Refresh-token expiry (UTC)
        });
    }

// -------- REGISTER --------
[HttpPost("register")]
[AllowAnonymous]
[ProducesResponseType(typeof(object), 201)]
[ProducesResponseType(typeof(object), 400)]
public async Task<IActionResult> Register([FromBody] AuthRegisterRequestDto dto, CancellationToken ct)
{
    if (!ModelState.IsValid) return ValidationProblem(ModelState);

    // 1) Kolla om e-post redan finns
    var existing = await _userMgr.FindByEmailAsync(dto.Email);
    if (existing != null)
        return BadRequest(new { message = "Email är redan registrerad." });

    // 2) Skapa Identity-användare
    var user = new IdentityUser
    {
        UserName = dto.Email.Trim(),
        Email = dto.Email.Trim(),
        EmailConfirmed = true // ev. sätt till false om du vill köra e-postverifiering
    };

    var create = await _userMgr.CreateAsync(user, dto.Password);
    if (!create.Succeeded)
        return BadRequest(new { errors = create.Errors.Select(e => e.Description) });

    // 3) Sätt standardroll
    await _userMgr.AddToRoleAsync(user, "Member");

    // 4) Skapa motsvarande Member i din domändatabas
    Guid memberId = Guid.Empty;
    try
    {
        var member = new Member
        {
            FirstName      = dto.FirstName?.Trim() ?? string.Empty,
            LastName       = dto.LastName?.Trim()  ?? string.Empty,
            Email          = dto.Email.Trim(),
            IdentityUserId = user.Id,
            CreatedAtUtc   = DateTime.UtcNow,
            IsActive       = true // om du har flaggan
        };

        _domainDb.Members.Add(member);
        await _domainDb.SaveChangesAsync(ct);
        memberId = member.Id;
    }
    catch
    {
        // Om domän-skapandet fallerar: ta bort Identity-användaren så vi inte hamnar i halvfärdigt läge
        await _userMgr.DeleteAsync(user);
        throw;
    }

    // 5) Svara 201 Created utan tokens (användaren får logga in separat)
    //    (Vill du, kan du använda Created(uri, body). Här kör vi enkel 201 + payload.)
    return StatusCode(StatusCodes.Status201Created, new
    {
        message  = "Konto skapat. Logga in för att erhålla token.",
        email    = user.Email,
        fullName = $"{dto.FirstName} {dto.LastName}".Trim(),
        memberId
    });
}

    // -------- REFRESH --------
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "refreshToken saknas." });

        var incomingHash = Hash(dto.RefreshToken);

        var token = await _authDb.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash, ct);

        if (token is null || !token.IsActive)
            return Unauthorized("Ogiltigt eller inaktivt refresh token.");

        // Rotera: revokera gammalt + skapa nytt
        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        token.RevokedReason = "Rotated on refresh";

        var newRaw = GenerateSecureRandomToken();
        var newHash = Hash(newRaw);
        var refreshExpires = DateTime.UtcNow.Add(RefreshTokenLifetime);

        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = token.UserId,
            TokenHash = newHash,
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        token.ReplacedByTokenHash = newHash;

        _authDb.RefreshTokens.Add(replacement);
        await _authDb.SaveChangesAsync(ct);

        var user = await _userMgr.FindByIdAsync(token.UserId);
        if (user is null) return Unauthorized("Användare saknas.");

        var member = await _domainDb.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdentityUserId == user.Id, ct);

        var (jwt, accessExpires) = await GenerateJwtAsync(user, member?.Id);

        return Ok(new
        {
            token = jwt,
            expires = accessExpires,
            refreshToken = newRaw,
            refreshExpires
        });
    }

    // -------- LOGOUT (revokera refresh token) --------
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] AuthRefreshRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { message = "refreshToken saknas." });

        var incomingHash = Hash(dto.RefreshToken);

        var token = await _authDb.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash, ct);

        if (token is null) return NoContent(); // redan ogiltig → OK

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        token.RevokedReason = "User logout";
        await _authDb.SaveChangesAsync(ct);

        return NoContent();
    }

    // -------- ME --------
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userMgr.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var roles = await _userMgr.GetRolesAsync(user);
        var member = await _domainDb.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdentityUserId == user.Id, ct);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            roles,
            memberId = member?.Id,
            memberName = member is null ? null : $"{member.FirstName} {member.LastName}".Trim()
        });
    }

    // ================= Helpers =================

    private async Task<(string jwt, DateTime expires)> GenerateJwtAsync(IdentityUser user, Guid? memberId)
    {
        var roles = await _userMgr.GetRolesAsync(user);
        var jwtCfg = _cfg.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtCfg["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email ?? "")
        };

        if (memberId.HasValue)
            claims.Add(new("memberId", memberId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expires = DateTime.UtcNow.Add(AccessTokenLifetime);

        var token = new JwtSecurityToken(
            issuer: jwtCfg["Issuer"],
            audience: jwtCfg["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    // Returnerar *både* raw refresh-token och dess expiry
    private async Task<(string raw, DateTime expires)> IssueRefreshTokenAsync(string userId, string? ip)
    {
        // valfritt: tillåt endast ett aktivt refresh-token per användare
        var actives = await _authDb.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        foreach (var t in actives)
        {
            t.RevokedAtUtc = DateTime.UtcNow;
            t.RevokedByIp = ip;
            t.RevokedReason = "Replaced on new login";
        }

        var raw = GenerateSecureRandomToken();
        var hash = Hash(raw);
        var expiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);

        _authDb.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ip
        });

        await _authDb.SaveChangesAsync();
        return (raw, expiresAt);
    }

    private static string GenerateSecureRandomToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
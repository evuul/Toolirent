using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TooliRent.Infrastructure.Data;
using TooliRent.Core.Models;

namespace TooliRent.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userMgr;
    private readonly IConfiguration _cfg;
    private readonly TooliRentDbContext _db;

    public AuthController(UserManager<IdentityUser> userMgr, IConfiguration cfg, TooliRentDbContext db)
    {
        _userMgr = userMgr;
        _cfg = cfg;
        _db = db;
    }

    // -------- LOGIN --------
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var user = await _userMgr.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized("Invalid login attempt.");

        var ok = await _userMgr.CheckPasswordAsync(user, dto.Password);
        if (!ok) return Unauthorized("Invalid login attempt.");

        // hämta ev. kopplad Member
        var member = await _db.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdentityUserId == user.Id, ct);

        var token = await GenerateJwtAsync(user, member?.Id);
        return Ok(new { token = token.jwt, expires = token.expires });
    }

    // -------- REGISTER --------
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        // enkel koll
        var existing = await _userMgr.FindByEmailAsync(dto.Email);
        if (existing != null) return BadRequest(new { message = "Email är redan registrerad." });

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var create = await _userMgr.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
            return BadRequest(new { errors = create.Errors.Select(e => e.Description) });

        // Lägg i Member-roll om den finns
        await _userMgr.AddToRoleAsync(user, "Member");

        // Skapa domänmedlem
        var member = new Member
        {
            FirstName      = (dto.FirstName ?? string.Empty).Trim(),
            LastName       = (dto.LastName  ?? string.Empty).Trim(),
            Email          = dto.Email.Trim(),
            IdentityUserId = user.Id,
            CreatedAtUtc   = DateTime.UtcNow
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync(ct);

        var token = await GenerateJwtAsync(user, member.Id);
        return Ok(new { token = token.jwt, expires = token.expires });
    }

    // -------- ME (valfritt men smidigt) --------
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userMgr.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var roles = await _userMgr.GetRolesAsync(user);
        var member = await _db.Members.AsNoTracking()
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

    // -------- Helpers --------
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

        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(
            issuer: jwtCfg["Issuer"],
            audience: jwtCfg["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

// ---------- DTOs ----------
public record LoginDto(string Email, string Password);

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.DTOs.Admins;
using TooliRent.Services.DTOs.Members;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.DTOs.Loans;     // LoanCheckoutDto, LoanDto
using TooliRent.Services.Exceptions;     // BatchReservationFailedException, ToolUnavailableException
using TooliRent.Services.Interfaces;

// +++ för revocation av refresh tokens + aktiv-check
using TooliRent.Infrastructure.Auth;        // AuthDbContext
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace TooliRent.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IMemberService _members;
    private readonly IReservationService _reservations;
    private readonly ILoanService _loans;

    // +++ för refresh-token revocation
    private readonly AuthDbContext _authDb;

    public AdminController(
        IAdminService admin,
        IMemberService members,
        IReservationService reservations,
        ILoanService loans,
        AuthDbContext authDb) // +++
    {
        _admin = admin;
        _members = members;
        _reservations = reservations;
        _loans = loans;
        _authDb = authDb; // +++
    }

    // ================== STATISTIK ==================
    [HttpGet("stats")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var stats = await _admin.GetStatsAsync(fromUtc, toUtc, ct);
        return Ok(stats);
    }

    // ================== MEDLEMMAR ==================
    [HttpGet("members")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GetMembers(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _members.SearchAsync(query, page, pageSize, ct);
        return Ok(new { total, items });
    }

    [HttpGet("members/{id:guid}")]
    [ProducesResponseType(typeof(MemberDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMemberById(Guid id, CancellationToken ct = default)
    {
        var m = await _members.GetAsync(id, ct);
        return m is null ? NotFound() : Ok(m);
    }

    [HttpPatch("members/{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetMemberStatus(
        Guid id,
        [FromBody] AdminSetMemberStatusDto body,
        CancellationToken ct = default)
    {
        if (body is null) return BadRequest(new { message = "Body krävs." });

        // Byt till atomiska varianten: sätter IsActive + bump:ar TokenVersion och returnerar IdentityUserId
        var (ok, identityUserId) = await _members.SetActiveAndBumpAsync(id, body.IsActive, ct);
        if (!ok) return NotFound();

        // Vid avaktivering: revokera alla aktiva refresh tokens för IdentityUserId
        if (!body.IsActive && !string.IsNullOrEmpty(identityUserId))
        {
            var now = DateTime.UtcNow;
            var ip  = HttpContext.Connection.RemoteIpAddress?.ToString();

            var tokens = _authDb.RefreshTokens
                .Where(t => t.UserId == identityUserId
                            && t.RevokedAtUtc == null
                            && t.ExpiresAtUtc > now);

            await foreach (var t in tokens.AsAsyncEnumerable().WithCancellation(ct))
            {
                t.RevokedAtUtc  = now;
                t.RevokedByIp   = ip;
                t.RevokedReason = "Member deactivated by admin";
            }
            await _authDb.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    [HttpPost("members/{id:guid}/deactivate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
        => Toggle(id, false, ct);

    [HttpPost("members/{id:guid}/activate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
        => Toggle(id, true, ct);

    private async Task<IActionResult> Toggle(Guid id, bool isActive, CancellationToken ct)
    {
        // återanvänd samma logik som i SetMemberStatus
        var (ok, identityUserId) = await _members.SetActiveAndBumpAsync(id, isActive, ct);
        if (!ok) return NotFound();

        if (!isActive && !string.IsNullOrEmpty(identityUserId))
        {
            var now = DateTime.UtcNow;
            var ip  = HttpContext.Connection.RemoteIpAddress?.ToString();

            var tokens = _authDb.RefreshTokens
                .Where(t => t.UserId == identityUserId
                            && t.RevokedAtUtc == null
                            && t.ExpiresAtUtc > now);

            await foreach (var t in tokens.AsAsyncEnumerable().WithCancellation(ct))
            {
                t.RevokedAtUtc  = now;
                t.RevokedByIp   = ip;
                t.RevokedReason = "Member deactivated by admin";
            }
            await _authDb.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    // ============ ADMIN: RESERVATIONER (BATCH) ============
    [HttpPost("reservations/verktyg")]
    [ProducesResponseType(typeof(ReservationBatchResultDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateReservationsBatchForMember(
        [FromBody] ReservationCreateDto dto,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (dto.MemberId is null || dto.MemberId == Guid.Empty)
            return BadRequest(new { message = "MemberId krävs för batch-skapande som admin." });

        // +++ Kolla att medlemmen är aktiv
        var m = await _members.GetAsync(dto.MemberId.Value, ct);
        if (m is null) return NotFound(new { message = "Medlem hittas inte." });
        if (!m.IsActive) return StatusCode(StatusCodes.Status423Locked, new { message = "Medlemmen är inaktiv." });

        try
        {
            var result = await _reservations.CreateBatchAsync(dto, ct);
            return Ok(result);
        }
        catch (BatchReservationFailedException ex)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Batch-reservation misslyckades",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            };
            problem.Extensions["availableToolIds"]   = ex.AvailableToolIds;
            problem.Extensions["unavailableToolIds"] = ex.UnavailableToolIds;
            return StatusCode(problem.Status!.Value, problem);
        }
        catch (ToolUnavailableException ex)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Ett eller flera verktyg är inte tillgängliga",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            };
            problem.Extensions["unavailableToolIds"] = ex.ToolIds;
            return StatusCode(problem.Status!.Value, problem);
        }
    }

    // ============ ADMIN: LÅN (BATCH CHECKOUT) ============
    /// <summary>
    /// Skapar ett eller flera lån i en batch för en specifik medlem.
    /// Body är en TOPP-NIVÅ ARRAY av LoanCheckoutDto:
    ///  - Via reservation: ange ReservationId (medlem verifieras mot memberId i URL).
    ///  - Direktlån: ange ToolIds + DueAtUtc (ett lån kan innehålla flera verktyg).
    /// </summary>
    [HttpPost("members/{memberId:guid}/loans/checkout-verktyg")]
    [ProducesResponseType(typeof(IEnumerable<LoanDto>), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CheckoutLoansBatchForAdmin(
        Guid memberId,
        [FromBody] IEnumerable<LoanCheckoutDto> items,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (items is null || !items.Any())
            return BadRequest(new { message = "Minst ett item krävs." });

        // +++ Kolla att medlemmen är aktiv
        var m = await _members.GetAsync(memberId, ct);
        if (m is null) return NotFound(new { message = "Medlem hittas inte." });
        if (!m.IsActive) return StatusCode(StatusCodes.Status423Locked, new { message = "Medlemmen är inaktiv." });

        try
        {
            var created = await _loans.CheckoutBatchForMemberAsync(items, memberId, ct);
            // vi har ingen specifik "GET loan" route här, så vi skickar 201 utan Location
            return Created(string.Empty, created);
        }
        catch (ToolUnavailableException ex)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Batch-utlåning misslyckades",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            };
            problem.Extensions["unavailableToolIds"] = ex.ToolIds;
            return StatusCode(problem.Status!.Value, problem);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Felaktig begäran",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ogiltig operation",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }

    // ============ ADMIN: RETURN ============
    [HttpPost("loans/{id:guid}/return")]
    [ProducesResponseType(typeof(LoanDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReturnLoanAsAdmin(
        Guid id,
        [FromBody] AdminLoanReturnDto body,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _loans.ReturnAsAdminAsync(id, body, ct);
        if (result is null) return NotFound();

        return Ok(result);
    }
}
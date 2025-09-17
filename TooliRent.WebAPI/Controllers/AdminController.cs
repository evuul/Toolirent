using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.DTOs.Admins;
using TooliRent.Services.DTOs.Members;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.DTOs.Loans;     // LoanCheckoutDto, LoanDto
using TooliRent.Services.Exceptions;     // BatchReservationFailedException, ToolUnavailableException
using TooliRent.Services.Interfaces;

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

    public AdminController(
        IAdminService admin,
        IMemberService members,
        IReservationService reservations,
        ILoanService loans)
    {
        _admin = admin;
        _members = members;
        _reservations = reservations;
        _loans = loans;
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

        var ok = await _members.SetActiveAsync(id, body.IsActive, ct);
        return ok ? NoContent() : NotFound();
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
        var ok = await _members.SetActiveAsync(id, isActive, ct);
        return ok ? NoContent() : NotFound();
    }

    // ============ ADMIN: RESERVATIONER (BATCH) ============
    [HttpPost("reservations/batch")]
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
    [HttpPost("members/{memberId:guid}/loans/checkout-batch")]
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
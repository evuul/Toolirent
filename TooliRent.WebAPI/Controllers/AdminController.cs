using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.DTOs.Admins;
using TooliRent.Services.DTOs.Members;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.DTOs.Loans;     // <-- för AdminLoanCheckoutDto & LoanDto
using TooliRent.Services.Exceptions;     // <-- BatchReservationFailedException, ev. ToolUnavailableException
using TooliRent.Services.Interfaces;

namespace TooliRent.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")] // Endast admin-användare
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IMemberService _members;
    private readonly IReservationService _reservations;
    private readonly ILoanService _loans;                  // <-- NYTT

    public AdminController(
        IAdminService admin,
        IMemberService members,
        IReservationService reservations,
        ILoanService loans)                                // <-- NYTT
    {
        _admin = admin;
        _members = members;
        _reservations = reservations;
        _loans = loans;                                    // <-- NYTT
    }

    // ====================================================
    // STATISTIK
    // ====================================================
    [HttpGet("stats")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
    {
        var stats = await _admin.GetStatsAsync(fromUtc, toUtc, ct);
        return Ok(stats);
    }

    // ====================================================
    // MEDLEMMAR – listning/sök
    // ====================================================
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

    // ====================================================
    // MEDLEM – hämta en
    // ====================================================
    [HttpGet("members/{id:guid}")]
    [ProducesResponseType(typeof(MemberDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMemberById(Guid id, CancellationToken ct)
    {
        var m = await _members.GetAsync(id, ct);
        return m is null ? NotFound() : Ok(m);
    }

    // ====================================================
    // MEDLEM – aktivera/inaktivera via body
    // ====================================================
    [HttpPatch("members/{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetMemberStatus(
        Guid id,
        [FromBody] AdminSetMemberStatusDto body,
        CancellationToken ct)
    {
        if (body is null) return BadRequest(new { message = "Body krävs." });

        var ok = await _members.SetActiveAsync(id, body.IsActive, ct);
        return ok ? NoContent() : NotFound();
    }

    // (valfria småendpoints utan body)
    [HttpPost("members/{id:guid}/deactivate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Toggle(id, false, ct);

    [HttpPost("members/{id:guid}/activate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => Toggle(id, true, ct);

    private async Task<IActionResult> Toggle(Guid id, bool isActive, CancellationToken ct)
    {
        var ok = await _members.SetActiveAsync(id, isActive, ct);
        return ok ? NoContent() : NotFound();
    }

    // ====================================================
    // ADMIN: SKAPA RESERVATIONER (BATCH, allt-eller-inget)
    // ====================================================
    [HttpPost("reservations/batch")]
    [ProducesResponseType(typeof(ReservationBatchResultDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateReservationsBatchForMember(
        [FromBody] ReservationBatchCreateDto dto,
        CancellationToken ct)
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
            return StatusCode(problem.Status.Value, problem);
        }
    }

    // ====================================================
    // ADMIN: SKAPA LÅN (BATCH, allt-eller-inget)
    // ====================================================
    /// <summary>
    /// Skapar ett eller flera lån i en batch.
    /// - Via reservation: ange ReservationId (Member härleds från reservationen).
    /// - Direktlån: ange ToolId + MemberId + DueAtUtc.
    /// Om en enda post inte går igenom avbryts hela batchen (allt-eller-inget).
    /// </summary>
    /// <remarks>
    /// Exempel (via reservation + direktlån i samma batch):
    /// POST /api/admin/loans/checkout-batch
    /// [
    ///   { "reservationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" },
    ///   { "toolId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "memberId": "cccccccc-cccc-cccc-cccc-cccccccccccc", "dueAtUtc": "2025-09-25T12:00:00Z" }
    /// ]
    /// </remarks>
    [HttpPost("loans/checkout-batch")]
    [ProducesResponseType(typeof(IEnumerable<LoanDto>), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CheckoutLoansBatchForAdmin(
        [FromBody] IEnumerable<AdminLoanCheckoutDto> items,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var created = await _loans.CheckoutBatchForAdminAsync(items, ct);
            return CreatedAtAction(nameof(GetStats), null, created);
        }
        catch (ToolUnavailableException ex)
        {
            // typiskt krock i fönster eller tool flaggat otillgängligt
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Batch-utlåning misslyckades",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            };
            return StatusCode(problem.Status.Value, problem);
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
    
    // ====================================================
    // LÅN (ADMIN): return
    // POST: api/admin/loans/{id}/return
    //
    // Body:
    //  { "loanId": "...", "returnedAtUtc": "...", "notes": "valfritt" }
    //
    // Admin kan sätta ReturnedAtUtc (även backdatera).
    // ====================================================
    [HttpPost("loans/{id:guid}/return")]
    [ProducesResponseType(typeof(LoanDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReturnLoanAsAdmin(
        Guid id,
        [FromBody] AdminLoanReturnDto body,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _loans.ReturnAsAdminAsync(id, body, ct);
        if (result is null) return NotFound();

        return Ok(result);
    }
}
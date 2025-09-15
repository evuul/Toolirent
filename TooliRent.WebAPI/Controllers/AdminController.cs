using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.DTOs.Admins;
using TooliRent.Services.DTOs.Members;
using TooliRent.Services.Interfaces;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IMemberService _members;

    public AdminController(IAdminService admin, IMemberService members)
    {
        _admin = admin;
        _members = members;
    }

    // =========================
    // Statistik
    // =========================
    [HttpGet("stats")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
    {
        var stats = await _admin.GetStatsAsync(fromUtc, toUtc, ct);
        return Ok(stats);
    }

    // =========================
    // Medlemmar: listning/sök
    // =========================
    [HttpGet("members")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetMembers(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _members.SearchAsync(query, page, pageSize, ct);
        return Ok(new { total, items });
    }

    // =========================
    // Medlem: hämta en
    // =========================
    [HttpGet("members/{id:guid}")]
    [ProducesResponseType(typeof(MemberDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMemberById(Guid id, CancellationToken ct)
    {
        var m = await _members.GetAsync(id, ct);
        return m is null ? NotFound() : Ok(m);
    }

    // =========================
    // Medlem: aktivera/inaktivera
    // =========================
    [HttpPatch("members/{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetMemberStatus(
        Guid id,
        [FromBody] AdminSetMemberStatusDto body,
        CancellationToken ct)
    {
        var ok = await _members.SetActiveAsync(id, body.IsActive, ct);
        return ok ? NoContent() : NotFound();
    }

    // (valfria “små” endpoints om du föredrar utan body)
    [HttpPost("members/{id:guid}/deactivate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Toggle(id, false, ct);

    [HttpPost("members/{id:guid}/activate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => Toggle(id, true, ct);

    private async Task<IActionResult> Toggle(Guid id, bool isActive, CancellationToken ct)
    {
        var ok = await _members.SetActiveAsync(id, isActive, ct);
        return ok ? NoContent() : NotFound();
    }
}
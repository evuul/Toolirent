using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.DTOs.Tools;
using TooliRent.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolService _tools;
    public ToolsController(IToolService tools) => _tools = tools;

    // GET: api/tools/search?categoryId=...&categoryName=...&isAvailable=...&query=...&page=1&pageSize=20
    [HttpGet("search")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? categoryName,
        [FromQuery] bool? isAvailable,
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _tools.SearchAsync(
            categoryId, isAvailable, query, page, pageSize, categoryName, ct);

        return Ok(new { total, items });
    }

    // GET: api/tools/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ToolDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tool = await _tools.GetAsync(id, ct);
        return tool is null ? NotFound() : Ok(tool);
    }

    // GET: api/tools/available?fromUtc=...&toUtc=...
    // Returns tools that are free (no overlapping reservations/loans) AND marked IsAvailable during the window.
    [HttpGet("available")]
    [ProducesResponseType(typeof(IEnumerable<ToolDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAvailableInWindow(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken ct = default)
    {
        if (toUtc <= fromUtc)
            return BadRequest("toUtc must be after fromUtc.");

        var items = await _tools.GetAvailableInWindowAsync(fromUtc, toUtc, ct);
        return Ok(items);
    }

    // POST: api/tools
    [HttpPost]
    [ProducesResponseType(typeof(ToolDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] ToolCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var created = await _tools.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT: api/tools/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ToolUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var ok = await _tools.UpdateAsync(id, dto, ct);
        return ok ? NoContent() : NotFound();
    }

    // DELETE: api/tools/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _tools.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
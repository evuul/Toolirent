// TooliRent.WebAPI/Controllers/ToolsController.cs

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolService _service;
    public ToolsController(IToolService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<object>> Search(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? categoryName,   // <— nytt
        [FromQuery] bool? isAvailable,
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _service.SearchAsync(
            categoryId, isAvailable, query, page, pageSize, categoryName, ct);

        return Ok(new { total, items });
    }
}
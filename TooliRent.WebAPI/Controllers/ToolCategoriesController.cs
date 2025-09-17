using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TooliRent.Services.DTOs.ToolCategories;
using TooliRent.Services.Interfaces;

namespace TooliRent.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolCategoriesController : ControllerBase
{
    private readonly IToolCategoryService _svc;

    public ToolCategoriesController(IToolCategoryService svc)
    {
        _svc = svc;
    }

    /// <summary>Lista alla kategorier</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ToolCategoryDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _svc.GetAllAsync(ct);
        return Ok(items);
    }
    
    /// <summary>Hämta kategori per Id</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ToolCategoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetAsync(id, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    /// <summary>Skapa ny kategori</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ToolCategoryDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] ToolCategoryCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Uppdatera kategori</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ToolCategoryUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var ok = await _svc.UpdateAsync(id, dto, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>Ta bort kategori (soft delete)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>Finns kategori-namn (case-insensitive)?</summary>
    [HttpGet("exists")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> NameExists([FromQuery] string name, [FromQuery] Guid? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "name is required" });

        var exists = await _svc.NameExistsAsync(name, excludeId, ct);
        return Ok(new { exists });
    }
}
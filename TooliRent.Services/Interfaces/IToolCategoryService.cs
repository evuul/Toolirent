using TooliRent.Core.Models;

namespace TooliRent.Services.Interfaces;

public interface IToolCategoryService
{
    /// <summary>Hämta alla kategorier.</summary>
    Task<IEnumerable<ToolCategory>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Hämta en kategori.</summary>
    Task<ToolCategory?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Skapa ny kategori. Kastar om namnet redan finns.</summary>
    Task<ToolCategory> CreateAsync(ToolCategory entity, CancellationToken ct = default);

    /// <summary>Uppdatera kategori. Kastar om namnet krockar med annan kategori.</summary>
    Task<bool> UpdateAsync(ToolCategory entity, CancellationToken ct = default);

    /// <summary>Ta bort kategori (hard delete eller soft beroende på repo-implementation).</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Kolla om namn finns (exkludera ett visst Id vid uppdatering).</summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IToolCategoryRepository : IRepository<ToolCategory>
{
    Task<bool> NameExistsAsync(string name, Guid? exceptId = null, CancellationToken ct = default);
    Task<ToolCategory?> GetByNameAsync(string name, CancellationToken ct = default);
}
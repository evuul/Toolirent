using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces.Repositories;

public interface IToolCategoryRepository : IRepository<ToolCategory>
{
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    Task<ToolCategory?> GetByNameAsync(string name, CancellationToken ct = default);

    // Kategori + antal verktyg (totalt och tillgängliga)
    Task<IEnumerable<(ToolCategory Category, int Total, int Available)>> GetWithCountsAsync(
        CancellationToken ct = default);
}
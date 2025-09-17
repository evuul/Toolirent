using TooliRent.Core.Models;

namespace TooliRent.Core.Interfaces;

public interface IMemberRepository : IRepository<Member>
{
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Member?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken ct = default);
    Task<Member?> GetWithReservationsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Member>> GetActiveAsync(CancellationToken ct = default);
    
    Task<(IEnumerable<Member> Items, int Total)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
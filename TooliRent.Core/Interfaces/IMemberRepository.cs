using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;

public interface IMemberRepository : IRepository<Member>
{
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Member?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken ct = default);
    Task<Member?> GetWithReservationsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Member>> GetActiveAsync(CancellationToken ct = default);

    Task<(IEnumerable<Member> Items, int Total)> SearchAsync(
        string? query, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Sätter IsActive och bump:ar TokenVersion i en atomisk operation.
    /// Returnerar true om en rad påverkades samt IdentityUserId för ev. token-revocation.
    /// </summary>
    Task<(bool Success, string? IdentityUserId)> SetActiveAndBumpTokenVersionAsync(
        Guid memberId, bool isActive, CancellationToken ct = default);
}
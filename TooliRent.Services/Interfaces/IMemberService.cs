using TooliRent.Core.Models;

namespace TooliRent.Services.Interfaces;

public interface IMemberService
{
    Task<IEnumerable<Member>> GetAllAsync(CancellationToken ct = default);
    Task<Member?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Member> CreateAsync(Member member, CancellationToken ct = default);
    Task<bool> UpdateAsync(Member member, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default); // soft delete
}
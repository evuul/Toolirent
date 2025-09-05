using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _uow;
    public MemberService(IUnitOfWork uow) => _uow = uow;

    public Task<IEnumerable<Member>> GetAllAsync(CancellationToken ct = default)
        => _uow.Members.GetAllAsync(ct);

    public Task<Member?> GetAsync(Guid id, CancellationToken ct = default)
        => _uow.Members.GetByIdAsync(id, ct);

    public async Task<Member> CreateAsync(Member member, CancellationToken ct = default)
    {
        member.FirstName = (member.FirstName ?? string.Empty).Trim();
        member.LastName  = (member.LastName  ?? string.Empty).Trim();
        member.Email     = (member.Email     ?? string.Empty).Trim();

        await _uow.Members.AddAsync(member, ct);
        await _uow.SaveChangesAsync(ct);
        return member;
    }

    public async Task<bool> UpdateAsync(Member member, CancellationToken ct = default)
    {
        var current = await _uow.Members.GetByIdAsync(member.Id, ct);
        if (current is null) return false;

        current.FirstName = (member.FirstName ?? string.Empty).Trim();
        current.LastName  = (member.LastName  ?? string.Empty).Trim();
        current.Email     = (member.Email     ?? string.Empty).Trim();

        await _uow.Members.UpdateAsync(current, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Members.GetByIdAsync(id, ct);
        if (entity is null) return false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        await _uow.Members.UpdateAsync(entity, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
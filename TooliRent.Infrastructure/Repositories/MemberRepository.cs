// TooliRent.Infrastructure/Repositories/MemberRepository.cs
using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.Repositories;

public class MemberRepository : Repository<Member>, IMemberRepository
{
    private readonly TooliRentDbContext _db;
    public MemberRepository(TooliRentDbContext db) : base(db) => _db = db;

    public async Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Members.FirstOrDefaultAsync(m => m.Email == email, ct);

    public async Task<Member?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken ct = default)
        => await _db.Members.FirstOrDefaultAsync(m => m.IdentityUserId == identityUserId, ct);

    public async Task<IEnumerable<Member>> GetActiveAsync(CancellationToken ct = default)
        => await _db.Members
            .AsNoTracking()
            .Where(m => m.DeletedAtUtc == null) // täcks också av global filter om du har det
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .ToListAsync(ct);

    public async Task<Member?> GetWithReservationsAsync(Guid id, CancellationToken ct = default)
        => await _db.Members
            .AsSplitQuery() // undvik kartesisk explosion när flera samlingar inkluderas
            .Include(m => m.Reservations)
            .ThenInclude(r => r.Items)
            .ThenInclude(i => i.Tool)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public override async Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);

    // NYTT: sök + pagination
    public async Task<(IEnumerable<Member> Items, int Total)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var q = _db.Members.AsNoTracking(); // global query filter tar bort soft-deletade

        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = $"%{query.Trim()}%";
            q = q.Where(m =>
                EF.Functions.Like(m.FirstName, like) ||
                EF.Functions.Like(m.LastName, like)  ||
                EF.Functions.Like(m.Email, like));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
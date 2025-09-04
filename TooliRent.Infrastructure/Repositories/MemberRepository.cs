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

    //  Aktiva medlemmar (dvs. ej soft-deletade)
    public async Task<IEnumerable<Member>> GetActiveAsync(CancellationToken ct = default)
        => await _db.Members
            .AsNoTracking()
            .Where(m => m.DeletedAtUtc == null)
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .ToListAsync(ct);

    // Hämta medlem inkl. reservationer (och ev. tool)
    public async Task<Member?> GetWithReservationsAsync(Guid id, CancellationToken ct = default)
        => await _db.Members
            .Include(m => m.Reservations)
            .ThenInclude(r => r.Tool)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    // (valfritt) override om du vill att “vanliga” GetById tar med reservationer:
    public override async Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Members
            .FirstOrDefaultAsync(m => m.Id == id, ct);
}
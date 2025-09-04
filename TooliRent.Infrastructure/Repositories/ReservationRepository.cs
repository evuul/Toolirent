using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Infrastructure.Data;

namespace TooliRent.Infrastructure.Repositories;

public class ReservationRepository : Repository<Reservation>, IReservationRepository
{
    private readonly TooliRentDbContext _db;
    public ReservationRepository(TooliRentDbContext db) : base(db) => _db = db;

    public override async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Reservations
            .Include(r => r.Tool)
            .Include(r => r.Member)
            .Include(r => r.Loan)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<Reservation>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
        => await _db.Reservations
            .AsNoTracking()
            .Where(r => r.MemberId == memberId && r.Status != ReservationStatus.Cancelled)
            .Include(r => r.Tool)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<bool> HasOverlapAsync(Guid toolId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        // overlap om: r.Start < to && r.End > from
        return await _db.Reservations
            .AsNoTracking()
            .AnyAsync(r =>
                r.ToolId == toolId &&
                r.Status == ReservationStatus.Active &&
                r.StartUtc < toUtc &&
                r.EndUtc   > fromUtc, ct);
    }

    public async Task MarkCancelledAsync(Reservation reservation, CancellationToken ct = default)
    {
        reservation.Status = ReservationStatus.Cancelled;
        await UpdateAsync(reservation, ct);
    }
}
// TooliRent.Infrastructure/Repositories/ReservationRepository.cs
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
            // Domännära “detalj”-hämtning: vi tar med det som ofta behövs för regler
            .Include(r => r.Member)
            .Include(r => r.Loan)
            .Include(r => r.Items).ThenInclude(i => i.Tool)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<Reservation>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
        => await _db.Reservations
            .AsNoTracking()
            .Where(r => r.MemberId == memberId && r.Status != ReservationStatus.Cancelled)
            .OrderByDescending(r => r.CreatedAtUtc)
            // Här tar vi inte med navigation by default – services kan välja att ladda mer vid behov
            .ToListAsync(ct);

    public async Task<bool> HasOverlapAsync(Guid toolId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        => await _db.Reservations
            .AsNoTracking()
            .AnyAsync(r =>
                r.Status == ReservationStatus.Active &&
                r.StartUtc < toUtc &&
                r.EndUtc   > fromUtc &&
                r.Items.Any(i => i.ToolId == toolId), ct);

    public async Task MarkCancelledAsync(Reservation reservation, CancellationToken ct = default)
    {
        reservation.Status = ReservationStatus.Cancelled;
        await UpdateAsync(reservation, ct);
    }
}
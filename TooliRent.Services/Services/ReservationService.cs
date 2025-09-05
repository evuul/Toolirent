using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _uow;
    public ReservationService(IUnitOfWork uow) => _uow = uow;

    public Task<Reservation?> GetAsync(Guid id, CancellationToken ct = default)
        => _uow.Reservations.GetByIdAsync(id, ct);

    public Task<IEnumerable<Reservation>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
        => _uow.Reservations.GetByMemberAsync(memberId, ct);

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        if (reservation.StartUtc >= reservation.EndUtc)
            throw new ArgumentException("End must be after Start");

        // Finns verktyget?
        var tool = await _uow.Tools.GetByIdAsync(reservation.ToolId, ct)
                   ?? throw new InvalidOperationException("Tool not found");

        // Överlapp?
        var overlap = await _uow.Reservations.HasOverlapAsync(reservation.ToolId, reservation.StartUtc, reservation.EndUtc, ct);
        if (overlap)
            throw new InvalidOperationException("Overlapping reservation exists for this tool");

        // Pris (enkel: hela dygn, minst 1)
        var days = Math.Max(1, (int)Math.Ceiling((reservation.EndUtc - reservation.StartUtc).TotalDays));
        reservation.TotalPrice = tool.RentalPricePerDay * days;
        reservation.IsPaid = false;
        reservation.Status = ReservationStatus.Active;

        await _uow.Reservations.AddAsync(reservation, ct);
        await _uow.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var res = await _uow.Reservations.GetByIdAsync(id, ct);
        if (res is null) return false;
        if (res.Status != ReservationStatus.Active) return false;

        res.Status = ReservationStatus.Cancelled;
        await _uow.Reservations.UpdateAsync(res, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> CompleteAsync(Guid id, Guid loanId, CancellationToken ct = default)
    {
        var res = await _uow.Reservations.GetByIdAsync(id, ct);
        if (res is null) return false;

        res.Status = ReservationStatus.Completed;
        await _uow.Reservations.UpdateAsync(res, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
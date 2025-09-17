using Microsoft.EntityFrameworkCore;
using TooliRent.Core.Enums;
using TooliRent.Infrastructure.Data;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.Interfaces;

namespace TooliRent.Infrastructure.Queries;

public class ReservationQueries : IReservationQueries
{
    private readonly TooliRentDbContext _db;
    public ReservationQueries(TooliRentDbContext db) => _db = db;

    public async Task<ReservationDto?> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                MemberId = r.MemberId,
                MemberName = r.Member != null
                    ? ((r.Member.FirstName ?? "").Trim() + " " + (r.Member.LastName ?? "").Trim()).Trim()
                    : string.Empty,
                StartUtc = r.StartUtc,
                EndUtc = r.EndUtc,
                Status = (int)r.Status,
                IsPaid = r.IsPaid,
                TotalPrice = r.TotalPrice,
                ItemCount = r.Items.Count,
                FirstToolName = r.Items
                    .OrderBy(i => i.CreatedAtUtc)
                    .Select(i => i.Tool != null ? i.Tool.Name : null)
                    .FirstOrDefault() ?? string.Empty,
                Items = r.Items
                    .OrderBy(i => i.CreatedAtUtc)
                    .Select(i => new ReservationItemDto(
                        i.ToolId,
                        i.Tool != null ? i.Tool.Name : string.Empty,
                        i.PricePerDay
                    ))
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ReservationDto>> GetActiveDtosForMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Reservations.AsNoTracking()
            .Where(r => r.MemberId == memberId && r.Status == ReservationStatus.Active && r.EndUtc >= now)
            .OrderBy(r => r.StartUtc)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                MemberId = r.MemberId,
                MemberName = r.Member != null
                    ? ((r.Member.FirstName ?? "").Trim() + " " + (r.Member.LastName ?? "").Trim()).Trim()
                    : string.Empty,
                StartUtc = r.StartUtc,
                EndUtc = r.EndUtc,
                Status = (int)r.Status,
                IsPaid = r.IsPaid,
                TotalPrice = r.TotalPrice,
                ItemCount = r.Items.Count,
                FirstToolName = r.Items
                    .OrderBy(i => i.CreatedAtUtc)
                    .Select(i => i.Tool != null ? i.Tool.Name : null)
                    .FirstOrDefault() ?? string.Empty,
                Items = new List<ReservationItemDto>() // tom i listvyn
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationDto>> GetHistoryDtosForMemberAsync(Guid memberId, int skip, int take, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Reservations.AsNoTracking()
            .Where(r => r.MemberId == memberId && (r.Status != ReservationStatus.Active || r.EndUtc < now))
            .OrderByDescending(r => r.EndUtc)
            .Skip(skip).Take(take)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                MemberId = r.MemberId,
                MemberName = r.Member != null
                    ? ((r.Member.FirstName ?? "").Trim() + " " + (r.Member.LastName ?? "").Trim()).Trim()
                    : string.Empty,
                StartUtc = r.StartUtc,
                EndUtc = r.EndUtc,
                Status = (int)r.Status,
                IsPaid = r.IsPaid,
                TotalPrice = r.TotalPrice,
                ItemCount = r.Items.Count,
                FirstToolName = r.Items
                    .OrderBy(i => i.CreatedAtUtc)
                    .Select(i => i.Tool != null ? i.Tool.Name : null)
                    .FirstOrDefault() ?? string.Empty,
                Items = new List<ReservationItemDto>()
            })
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<ReservationDto> Items, int Total)> AdminSearchDtosAsync(
        DateTime? fromUtc, DateTime? toUtc, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var q = _db.Reservations.AsNoTracking().Where(r => r.DeletedAtUtc == null);

        if (fromUtc.HasValue) q = q.Where(r => r.StartUtc >= fromUtc.Value);
        if (toUtc.HasValue)   q = q.Where(r => r.StartUtc <  toUtc.Value);
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ReservationStatus>(status, true, out var s))
        {
            q = q.Where(r => r.Status == s);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(r => r.StartUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                MemberId = r.MemberId,
                MemberName = r.Member != null
                    ? ((r.Member.FirstName ?? "").Trim() + " " + (r.Member.LastName ?? "").Trim()).Trim()
                    : string.Empty,
                StartUtc = r.StartUtc,
                EndUtc = r.EndUtc,
                Status = (int)r.Status,
                IsPaid = r.IsPaid,
                TotalPrice = r.TotalPrice,
                ItemCount = r.Items.Count,
                FirstToolName = r.Items
                    .OrderBy(i => i.CreatedAtUtc)
                    .Select(i => i.Tool != null ? i.Tool.Name : null)
                    .FirstOrDefault() ?? string.Empty,
                Items = new List<ReservationItemDto>()
            })
            .ToListAsync(ct);

        return (items, total);
    }
}
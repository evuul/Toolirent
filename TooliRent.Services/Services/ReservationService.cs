// TooliRent.Services/Services/ReservationService.cs
using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;           // IUnitOfWork, repos
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.Exceptions;       // ToolUnavailableException
using TooliRent.Services.Interfaces;       // IReservationService, IReservationQueries

namespace TooliRent.Services.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _uow;
    private readonly IReservationQueries _queries;
    private readonly IMapper _mapper;

    public ReservationService(IUnitOfWork uow, IReservationQueries queries, IMapper mapper)
    {
        _uow = uow;
        _queries = queries;
        _mapper = mapper;
    }

    // ========= CREATE: EN reservation med flera items =========
    public async Task<ReservationDto> CreateAsync(ReservationCreateDto dto, CancellationToken ct)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var toolIds = dto.ToolIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (toolIds.Length == 0) throw new ArgumentException("Minst ett verktyg måste väljas.");
        if (dto.EndUtc <= dto.StartUtc) throw new ArgumentException("EndUtc måste vara efter StartUtc.");
        if (dto.MemberId is null || dto.MemberId == Guid.Empty) throw new ArgumentException("MemberId saknas.");

        // 1) Hämta verktyg
        var tools = new List<Tool>(toolIds.Length);
        foreach (var id in toolIds)
        {
            var t = await _uow.Tools.GetByIdAsync(id, ct);
            if (t != null) tools.Add(t);
        }

        // 2) Saknade / flaggade otillgängliga
        var missing = toolIds.Except(tools.Select(t => t.Id)).ToList();
        var flaggedUnavailable = tools.Where(t => !t.IsAvailable).Select(t => t.Id).ToList();

        // 3) Tillgänglighet i fönster (kollar mot ReservationItems + LoanItems)
        var availability = await _uow.Tools.AreAvailableInWindowAsync(toolIds, dto.StartUtc, dto.EndUtc, null, null, ct);
        var windowUnavailable = availability.Where(kv => kv.Value == false).Select(kv => kv.Key).ToList();

        var unavailable = missing
            .Concat(flaggedUnavailable)
            .Concat(windowUnavailable)
            .Distinct()
            .ToList();

        if (unavailable.Count > 0)
            throw new ToolUnavailableException(unavailable, "Ett eller flera verktyg är inte tillgängliga för valt tidsintervall.");

        // 4) Pris & items
        var days = Math.Max(1, (int)Math.Ceiling((dto.EndUtc - dto.StartUtc).TotalDays));
        var items = tools.Select(t => new ReservationItem
        {
            ToolId      = t.Id,
            PricePerDay = t.RentalPricePerDay
        }).ToList();

        var total = items.Sum(i => i.PricePerDay * days);

        // 5) Skapa entity
        var entity = new Reservation
        {
            MemberId   = dto.MemberId.Value,
            StartUtc   = dto.StartUtc,
            EndUtc     = dto.EndUtc,
            Status     = ReservationStatus.Active,
            IsPaid     = false,
            TotalPrice = total,
            Items      = items
        };

        await _uow.Reservations.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // 6) Returnera DTO (projekterad via queries)
        var dtoOut = await _queries.GetDtoByIdAsync(entity.Id, ct);
        return dtoOut ?? _mapper.Map<ReservationDto>(entity);
    }

    // ========= CREATE BATCH: Admin skapar EN multi-item reservation =========
    public async Task<ReservationBatchResultDto> CreateBatchAsync(ReservationCreateDto dto, CancellationToken ct)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var toolIds = dto.ToolIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (toolIds.Length == 0) throw new ArgumentException("Minst ett verktyg måste väljas.");
        if (dto.EndUtc <= dto.StartUtc) throw new ArgumentException("EndUtc måste vara efter StartUtc.");
        if (dto.MemberId is null || dto.MemberId == Guid.Empty) throw new ArgumentException("MemberId saknas.");

        // 1) Hämta verktyg
        var tools = new List<Tool>(toolIds.Length);
        foreach (var id in toolIds)
        {
            var t = await _uow.Tools.GetByIdAsync(id, ct);
            if (t != null) tools.Add(t);
        }

        // 2) Saknade / flaggade otillgängliga
        var missing = toolIds.Except(tools.Select(t => t.Id)).ToList();
        var flaggedUnavailable = tools.Where(t => !t.IsAvailable).Select(t => t.Id).ToList();

        // 3) Tillgänglighet i fönster
        var availability = await _uow.Tools.AreAvailableInWindowAsync(toolIds, dto.StartUtc, dto.EndUtc, null, null, ct);
        var windowUnavailable = availability.Where(kv => kv.Value == false).Select(kv => kv.Key).ToList();

        var unavailable = missing
            .Concat(flaggedUnavailable)
            .Concat(windowUnavailable)
            .Distinct()
            .ToList();

        if (unavailable.Count > 0)
            throw new ToolUnavailableException(unavailable, "Ett eller flera verktyg är inte tillgängliga för valt tidsintervall.");

        // 4) Pris & items
        var days = Math.Max(1, (int)Math.Ceiling((dto.EndUtc - dto.StartUtc).TotalDays));
        var items = tools.Select(t => new ReservationItem
        {
            ToolId      = t.Id,
            PricePerDay = t.RentalPricePerDay
        }).ToList();

        var total = items.Sum(i => i.PricePerDay * days);

        // 5) Skapa EN reservation med alla items
        var entity = new Reservation
        {
            MemberId   = dto.MemberId.Value,
            StartUtc   = dto.StartUtc,
            EndUtc     = dto.EndUtc,
            Status     = ReservationStatus.Active,
            IsPaid     = false,
            TotalPrice = total,
            Items      = items
        };

        await _uow.Reservations.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // 6) Hämta projekterad DTO för snygg output
        var createdDto = await _queries.GetDtoByIdAsync(entity.Id, ct)
                         ?? _mapper.Map<ReservationDto>(entity);

        // 7) Returnera batch-result som innehåller EN reservation
        return new ReservationBatchResultDto(
            MemberId: dto.MemberId.Value,
            StartUtc: dto.StartUtc,
            EndUtc:   dto.EndUtc,
            Reservations: new[] { createdDto }
        );
    }

    // ========= READ: admin – detalj =========
    public Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken ct)
        => _queries.GetDtoByIdAsync(id, ct);

    // ========= READ: member – detalj med ägarkrav =========
    public async Task<ReservationDto?> GetForMemberAsync(Guid id, Guid memberId, CancellationToken ct)
    {
        var dto = await _queries.GetDtoByIdAsync(id, ct);
        if (dto is null || dto.MemberId != memberId) return null;
        return dto;
    }

    // ========= READ: member – aktiva =========
    public Task<IReadOnlyList<ReservationDto>> GetActiveForMemberAsync(Guid memberId, CancellationToken ct)
        => _queries.GetActiveDtosForMemberAsync(memberId, ct);

    // ========= READ: member – historik =========
    public Task<IReadOnlyList<ReservationDto>> GetHistoryForMemberAsync(Guid memberId, int skip, int take, CancellationToken ct)
        => _queries.GetHistoryDtosForMemberAsync(memberId, skip, take, ct);

    // ========= CANCEL: member/admin =========
    public async Task<bool> CancelAsync(Guid id, Guid? actingMemberId, CancellationToken ct)
    {
        var res = await _uow.Reservations.GetByIdAsync(id, ct);
        if (res is null) return false;

        // Ägarkrav om det är medlem
        if (actingMemberId is Guid mid && res.MemberId != mid) return false;

        if (res.Status != ReservationStatus.Active) return false;

        res.Status = ReservationStatus.Cancelled;
        await _uow.Reservations.UpdateAsync(res, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
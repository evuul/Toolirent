using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ReservationService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ReservationDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _uow.Reservations.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<ReservationDto>(entity);
    }


    public async Task<IEnumerable<ReservationDto>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var list = await _uow.Reservations.GetByMemberAsync(memberId, ct);
        return _mapper.Map<IEnumerable<ReservationDto>>(list);
    }

    public async Task<ReservationDto> CreateAsync(ReservationCreateDto dto, CancellationToken ct = default)
    {
        // 1) Kolla verktyget
        var tool = await _uow.Tools.GetByIdAsync(dto.ToolId, ct);
        if (tool is null) throw new InvalidOperationException("Tool not found.");
        if (!tool.IsAvailable) throw new InvalidOperationException("Tool is not available.");

        // 2) datumvalid
        if (dto.EndUtc <= dto.StartUtc)
            throw new ArgumentException("EndUtc must be after StartUtc.");

        // 3) Överlapp mot andra reservationer
        var overlap = await _uow.Reservations.HasOverlapAsync(dto.ToolId, dto.StartUtc, dto.EndUtc, ct);
        if (overlap) throw new InvalidOperationException("Tool already reserved in this window.");

        // 4) Pris = antal dagar * pris/dag
        var days = Math.Max(1, (int)Math.Ceiling((dto.EndUtc - dto.StartUtc).TotalDays));
        var total = days * tool.RentalPricePerDay;

        var entity = _mapper.Map<Reservation>(dto);
        entity.TotalPrice = total;
        entity.IsPaid = false;
        entity.Status = ReservationStatus.Active;

        await _uow.Reservations.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // Ladda om för mappning med namn
        var created = await _uow.Reservations.GetByIdAsync(entity.Id, ct);
        return _mapper.Map<ReservationDto>(created!);
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
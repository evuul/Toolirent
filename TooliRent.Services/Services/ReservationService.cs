using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Reservations;
using TooliRent.Services.Exceptions;  // ToolUnavailableException
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
        // 1) Hämta verktyget
        var tool = await _uow.Tools.GetByIdAsync(dto.ToolId, ct);
        if (tool is null)
            throw new InvalidOperationException("Tool not found.");

        // 2) Grundvalidering av datum
        if (dto.EndUtc <= dto.StartUtc)
            throw new ArgumentException("EndUtc måste vara efter StartUtc.");
        
        // 3) Snabb flagg-koll (t.ex. trasigt/ur bruk)
        if (!tool.IsAvailable)
            throw new ToolUnavailableException("Verktyget är markerat som otillgängligt.");

        // 4) Central koll av fönstertillgänglighet (täcker både andra reservationer och pågående lån)
        var isFree = await _uow.Tools.IsAvailableInWindowAsync(dto.ToolId, dto.StartUtc, dto.EndUtc, ct);
        if (!isFree)
            throw new ToolUnavailableException("Verktyget är inte tillgängligt i valt tidsintervall.");

        // 5) Prisberäkning: antal hela dagar (minst 1) * pris/dag
        var days = Math.Max(1, (int)Math.Ceiling((dto.EndUtc - dto.StartUtc).TotalDays));
        var total = days * tool.RentalPricePerDay;

        var entity = _mapper.Map<Reservation>(dto);
        entity.TotalPrice = total;
        entity.IsPaid = false;
        entity.Status = ReservationStatus.Active;

        await _uow.Reservations.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // 6) Ladda om för att mappa med navigationer/namn
        var created = await _uow.Reservations.GetByIdAsync(entity.Id, ct);
        return _mapper.Map<ReservationDto>(created!);
    }
    
    public async Task<ReservationBatchResultDto> CreateBatchAsync(ReservationBatchCreateDto dto, CancellationToken ct = default)
    {
        var items = new List<ReservationBatchItemResultDto>();

        // Basvalidering av fönster
        if (dto.EndUtc <= dto.StartUtc)
            throw new ArgumentException("EndUtc måste vara efter StartUtc.");

        foreach (var toolId in dto.ToolIds.Distinct())
        {
            try
            {
                // Återanvänd din vanliga CreateAsync – NOTERA att MemberId redan är satt/överskriven i controllern
                var single = new ReservationCreateDto(
                    ToolId: toolId,
                    MemberId: dto.MemberId!.Value, // kommer från controller
                    StartUtc: dto.StartUtc,
                    EndUtc: dto.EndUtc
                );

                var created = await CreateAsync(single, ct);

                items.Add(new ReservationBatchItemResultDto(
                    ToolId: toolId,
                    Success: true,
                    Error: null,
                    Reservation: created
                ));
            }
            catch (ToolUnavailableException ex)
            {
                items.Add(new ReservationBatchItemResultDto(
                    ToolId: toolId,
                    Success: false,
                    Error: ex.Message,
                    Reservation: null
                ));
            }
            catch (Exception ex)
            {
                // Fånga övriga fel per item – batch ska inte dö helt för ett fel
                items.Add(new ReservationBatchItemResultDto(
                    ToolId: toolId,
                    Success: false,
                    Error: ex.Message,
                    Reservation: null
                ));
            }
        }

        return new ReservationBatchResultDto(
            MemberId: dto.MemberId!.Value,
            StartUtc: dto.StartUtc,
            EndUtc: dto.EndUtc,
            Items: items
        );
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
        // (valfritt) koppla loanId till res om du vill spara spårning
        await _uow.Reservations.UpdateAsync(res, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
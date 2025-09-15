// TooliRent.Services/Services/ReservationService.cs
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
    
    /// <summary>
    /// Skapar flera reservationer i ett svep (ALL-OR-NOTHING).
    /// Om något verktyg inte går att boka → ingen reservation sparas.
    /// </summary>
    public async Task<ReservationBatchResultDto> CreateBatchAsync(ReservationBatchCreateDto dto, CancellationToken ct = default)
    {
        // 1) Basvalidering
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        var toolIds = dto.ToolIds?.Distinct().ToList() ?? new List<Guid>();
        if (toolIds.Count == 0) throw new ArgumentException("Minst ett verktyg måste väljas.");
        if (dto.EndUtc <= dto.StartUtc) throw new ArgumentException("EndUtc måste vara efter StartUtc.");
        if (!dto.MemberId.HasValue || dto.MemberId.Value == Guid.Empty)
            throw new ArgumentException("MemberId saknas.");

        // 2) Plocka alla efterfrågade verktyg
        var tools = new List<Tool>();
        foreach (var id in toolIds)
        {
            var t = await _uow.Tools.GetByIdAsync(id, ct);
            if (t != null) tools.Add(t);
        }

        // 3) Lista vilka som saknas helt
        var missing = toolIds.Except(tools.Select(t => t.Id)).ToList();
        // 4) Kolla flaggan IsAvailable
        var flaggedUnavailable = tools.Where(t => !t.IsAvailable).Select(t => t.Id).ToList();

        // 5) Kolla fönstertillgänglighet (res + öppna lån)
        var unavailableByWindow = new List<Guid>();
        foreach (var tool in tools)
        {
            var ok = await _uow.Tools.IsAvailableInWindowAsync(tool.Id, dto.StartUtc, dto.EndUtc, ct);
            if (!ok) unavailableByWindow.Add(tool.Id);
        }

        // 6) Summera allt som är “inte bokningsbart”
        var unavailable = missing
            .Concat(flaggedUnavailable)
            .Concat(unavailableByWindow)
            .Distinct()
            .ToList();

        // 7) Om någon är otillgänglig → kasta exception (ALL-OR-NOTHING)
        if (unavailable.Count > 0)
        {
            var available = toolIds.Except(unavailable).ToList();
            throw new BatchReservationFailedException(
                available,
                unavailable,
                "Batch-reservationen kunde inte genomföras eftersom ett eller flera verktyg inte är tillgängliga."
            );
        }

        // 8) Alla är bokningsbara → skapa reservationer (vi sparar EN gång på slutet)
        var createdEntities = new List<Reservation>();
        foreach (var tool in tools)
        {
            var days = Math.Max(1, (int)Math.Ceiling((dto.EndUtc - dto.StartUtc).TotalDays));
            var total = days * tool.RentalPricePerDay;

            var entity = new Reservation
            {
                Id         = Guid.NewGuid(),
                ToolId     = tool.Id,
                MemberId   = dto.MemberId!.Value,
                StartUtc   = dto.StartUtc,
                EndUtc     = dto.EndUtc,
                TotalPrice = total,
                IsPaid     = false,
                Status     = ReservationStatus.Active,
            };

            await _uow.Reservations.AddAsync(entity, ct);
            createdEntities.Add(entity);
        }

        await _uow.SaveChangesAsync(ct);

        // 9) Ladda om för mapping inkl. navigationer (valfritt, men bra för ToolName/MemberName)
        var createdDtos = new List<ReservationDto>();
        foreach (var e in createdEntities)
        {
            var fresh = await _uow.Reservations.GetByIdAsync(e.Id, ct);
            createdDtos.Add(_mapper.Map<ReservationDto>(fresh!));
        }

        return new ReservationBatchResultDto(
            MemberId: dto.MemberId!.Value,
            StartUtc: dto.StartUtc,
            EndUtc: dto.EndUtc,
            Reservations: createdDtos
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
        await _uow.Reservations.UpdateAsync(res, ct);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
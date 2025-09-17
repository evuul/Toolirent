using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Loans;
using TooliRent.Services.Exceptions; // ToolUnavailableException
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services
{
    public class LoanService : ILoanService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public LoanService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // -------------------------------------------------
        // Hämta ett enskilt lån
        // -------------------------------------------------
        public async Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            return loan is null ? null : _mapper.Map<LoanDto>(loan);
        }

        // =====================================================================
        // MEDLEM: Batch-checkout (MemberId från JWT i controller)
        //  - Varje item: antingen ReservationId ELLER (ToolId + DueAtUtc)
        //  - Allt eller inget
        // =====================================================================
        public async Task<IEnumerable<LoanDto>> CheckoutBatchForMemberAsync(
            IEnumerable<LoanCheckoutDto> items,
            Guid currentMemberId,
            CancellationToken ct = default)
        {
            var now        = DateTime.UtcNow;
            var toCreate   = new List<Loan>();
            var toolsTaken = new HashSet<Guid>(); // skydd mot duplicerade toolId i samma batch

            // Validera alla poster först (inget skrivs än)
            foreach (var it in items)
            {
                if (it.ReservationId is Guid rid)
                {
                    // ---- Lån baserat på RESERVATION (multi-item) ----
                    var res = await _uow.Reservations.GetByIdAsync(rid, ct)
                              ?? throw new InvalidOperationException("Reservation not found.");

                    if (res.Status != ReservationStatus.Active)
                        throw new InvalidOperationException("Reservation is not active.");

                    if (res.MemberId != currentMemberId)
                        throw new UnauthorizedAccessException("Reservationen tillhör inte denna medlem.");

                    if (now < res.StartUtc)
                        throw new InvalidOperationException($"Kan inte checka ut före {res.StartUtc:yyyy-MM-dd HH:mm} UTC.");
                    if (now >= res.EndUtc)
                        throw new InvalidOperationException("Reservationens sluttid har passerat.");

                    if (res.Items == null || res.Items.Count == 0)
                        throw new InvalidOperationException("Reservationen saknar items.");

                    // dueAt = input eller reservationens slut
                    var dueAt = it.DueAtUtc ?? res.EndUtc;
                    if (dueAt <= now)
                        throw new InvalidOperationException("DueAtUtc/EndUtc måste vara i framtiden.");

                    // Kolla dubbletter i samma batch (per verktyg)
                    foreach (var item in res.Items)
                        if (!toolsTaken.Add(item.ToolId))
                            throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                    // Tillgänglighet för ALLA tools i reservationen (ignorera den här reservationen)
                    var toolIds = res.Items.Select(x => x.ToolId).Distinct().ToArray();
                    var avail = await _uow.Tools.AreAvailableInWindowAsync(
                        toolIds, now, dueAt,
                        ignoreReservationId: res.Id,
                        ignoreLoanId: null,
                        ct: ct);

                    var unavailable = avail.Where(kv => kv.Value == false).Select(kv => kv.Key).ToList();
                    if (unavailable.Count > 0)
                        throw new ToolUnavailableException(unavailable, "Ett eller flera verktyg är inte tillgängliga i valt tidsintervall.");

                    // Skapa Loan + LoanItems från reservationens items
                    var days = Math.Max(1, (int)Math.Ceiling((dueAt - now).TotalDays));
                    var loanItems = res.Items.Select(i => new LoanItem
                    {
                        ToolId      = i.ToolId,
                        PricePerDay = i.PricePerDay
                    }).ToList();

                    var totalPerDay = loanItems.Sum(li => li.PricePerDay);
                    var loan = new Loan
                    {
                        MemberId         = res.MemberId,
                        ReservationId    = res.Id,
                        CheckedOutAtUtc  = now,
                        DueAtUtc         = dueAt,
                        Status           = LoanStatus.Open,
                        Items            = loanItems,
                        TotalPrice       = totalPerDay * days
                    };

                    // Markera reservationen som Completed (skrivs vid SaveChanges)
                    res.Status = ReservationStatus.Completed;
                    await _uow.Reservations.UpdateAsync(res, ct);

                    toCreate.Add(loan);
                }
                else
                {
                    // ---- DIREKTLÅN (multi-item loan) ----
                    if (it.ToolIds is null || !it.ToolIds.Any() || it.DueAtUtc is null)
                        throw new ArgumentException("ToolIds och DueAtUtc krävs för direktlån.");

                    var dueAt = it.DueAtUtc.Value;

                    var loanItems = new List<LoanItem>();
                    foreach (var toolId in it.ToolIds)
                    {
                        var tool = await _uow.Tools.GetByIdAsync(toolId, ct)
                                   ?? throw new InvalidOperationException("Tool not found.");

                        if (!toolsTaken.Add(toolId))
                            throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                        if (!tool.IsAvailable)
                            throw new ToolUnavailableException(new[] { toolId }, "Verktyget är markerat som otillgängligt.");

                        var free = await _uow.Tools.IsAvailableInWindowAsync(toolId, now, dueAt, ct);
                        if (!free)
                            throw new ToolUnavailableException(new[] { toolId }, "Verktyget är inte tillgängligt i valt tidsintervall.");

                        loanItems.Add(new LoanItem { ToolId = tool.Id, PricePerDay = tool.RentalPricePerDay });
                    }

// Skapa själva lånet
                    var days = Math.Max(1, (int)Math.Ceiling((dueAt - now).TotalDays));
                    var totalPerDay = loanItems.Sum(li => li.PricePerDay);

                    var loan = new Loan
                    {
                        MemberId        = currentMemberId,
                        CheckedOutAtUtc = now,
                        DueAtUtc        = dueAt,
                        Status          = LoanStatus.Open,
                        Items           = loanItems,
                        TotalPrice      = totalPerDay * days
                    };

                    toCreate.Add(loan);
                }
            }

            // 2) Skriv alla i ett svep
            foreach (var l in toCreate)
                await _uow.Loans.AddAsync(l, ct);

            await _uow.SaveChangesAsync(ct);

            // 3) Returnera med navigationer
            var result = new List<LoanDto>();
            foreach (var l in toCreate)
            {
                var fresh = await _uow.Loans.GetByIdAsync(l.Id, ct);
                result.Add(_mapper.Map<LoanDto>(fresh!));
            }
            return result;
        }

        // =====================================================================
        // ADMIN: Batch-checkout (MemberId per item eller från reservation)
        // =====================================================================
        public async Task<IEnumerable<LoanDto>> CheckoutBatchForAdminAsync(
            IEnumerable<AdminLoanCheckoutDto> items,
            CancellationToken ct = default)
        {
            var now        = DateTime.UtcNow;
            var toCreate   = new List<Loan>();
            var toolsTaken = new HashSet<Guid>();

            foreach (var it in items)
            {
                if (it.ReservationId is Guid rid)
                {
                    // ---- Via reservation (multi-item) ----
                    var res = await _uow.Reservations.GetByIdAsync(rid, ct)
                              ?? throw new InvalidOperationException("Reservation not found.");
                    if (res.Status != ReservationStatus.Active)
                        throw new InvalidOperationException("Reservation is not active.");
                    if (res.Items == null || res.Items.Count == 0)
                        throw new InvalidOperationException("Reservationen saknar items.");

                    if (now < res.StartUtc)
                        throw new InvalidOperationException($"Kan inte checka ut före {res.StartUtc:yyyy-MM-dd HH:mm} UTC.");
                    if (now >= res.EndUtc)
                        throw new InvalidOperationException("Reservationens sluttid har passerat.");

                    var dueAt = it.DueAtUtc ?? res.EndUtc;
                    if (dueAt <= now)
                        throw new InvalidOperationException("DueAtUtc/EndUtc måste vara i framtiden.");

                    foreach (var item in res.Items)
                        if (!toolsTaken.Add(item.ToolId))
                            throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                    var toolIds = res.Items.Select(x => x.ToolId).Distinct().ToArray();
                    var avail = await _uow.Tools.AreAvailableInWindowAsync(
                        toolIds, now, dueAt,
                        ignoreReservationId: res.Id,
                        ignoreLoanId: null,
                        ct: ct);

                    var unavailable = avail.Where(kv => kv.Value == false).Select(kv => kv.Key).ToList();
                    if (unavailable.Count > 0)
                        throw new ToolUnavailableException(unavailable, "Ett eller flera verktyg är inte tillgängliga i valt tidsintervall.");

                    var days = Math.Max(1, (int)Math.Ceiling((dueAt - now).TotalDays));
                    var loanItems = res.Items.Select(i => new LoanItem
                    {
                        ToolId      = i.ToolId,
                        PricePerDay = i.PricePerDay
                    }).ToList();

                    var totalPerDay = loanItems.Sum(li => li.PricePerDay);
                    var loan = new Loan
                    {
                        MemberId        = res.MemberId, // från reservation
                        ReservationId   = res.Id,
                        CheckedOutAtUtc = now,
                        DueAtUtc        = dueAt,
                        Status          = LoanStatus.Open,
                        Items           = loanItems,
                        TotalPrice      = totalPerDay * days
                    };

                    res.Status = ReservationStatus.Completed;
                    await _uow.Reservations.UpdateAsync(res, ct);

                    toCreate.Add(loan);
                }
                else
                {
                    // ---- Direktlån (single-item) ----
                    if (it.ToolId is null || it.MemberId is null || it.DueAtUtc is null)
                        throw new ArgumentException("ToolId, MemberId och DueAtUtc krävs för direktlån.");

                    var toolId   = it.ToolId.Value;
                    var memberId = it.MemberId.Value;
                    var dueAt    = it.DueAtUtc.Value;

                    var tool = await _uow.Tools.GetByIdAsync(toolId, ct)
                               ?? throw new InvalidOperationException("Tool not found.");
                    var member = await _uow.Members.GetByIdAsync(memberId, ct)
                                 ?? throw new InvalidOperationException("Member not found.");

                    if (!toolsTaken.Add(toolId))
                        throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                    if (!tool.IsAvailable)
                        throw new ToolUnavailableException(new[] { toolId }, "Verktyget är markerat som otillgängligt.");

                    if (dueAt <= now)
                        throw new ArgumentException("DueAtUtc måste vara i framtiden.");

                    var free = await _uow.Tools.IsAvailableInWindowAsync(toolId, now, dueAt, ct);
                    if (!free)
                        throw new ToolUnavailableException(new[] { toolId }, "Verktyget är inte tillgängligt i valt tidsintervall.");

                    var days = Math.Max(1, (int)Math.Ceiling((dueAt - now).TotalDays));
                    var loan = new Loan
                    {
                        MemberId        = member.Id,
                        CheckedOutAtUtc = now,
                        DueAtUtc        = dueAt,
                        Status          = LoanStatus.Open,
                        Items           = new List<LoanItem>
                        {
                            new LoanItem { ToolId = tool.Id, PricePerDay = tool.RentalPricePerDay }
                        },
                        TotalPrice      = tool.RentalPricePerDay * days
                    };

                    toCreate.Add(loan);
                }
            }

            foreach (var l in toCreate)
                await _uow.Loans.AddAsync(l, ct);

            await _uow.SaveChangesAsync(ct);

            var result = new List<LoanDto>();
            foreach (var l in toCreate)
            {
                var fresh = await _uow.Loans.GetByIdAsync(l.Id, ct);
                result.Add(_mapper.Map<LoanDto>(fresh!));
            }
            return result;
        }

        // =====================================================================
        // RETUR: Medlem
        // =====================================================================
        public async Task<LoanDto?> ReturnAsMemberAsync(
            Guid id,
            Guid currentMemberId,
            LoanReturnDto dto,
            CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            if (loan is null) return null;
            if (loan.MemberId != currentMemberId) return null;

            if (loan.Status == LoanStatus.Returned || loan.Status == LoanStatus.Late)
                return _mapper.Map<LoanDto>(loan);

            var returnedAt = DateTime.UtcNow;
            loan.ReturnedAtUtc = returnedAt;
            loan.Notes = dto.Notes;

            if (returnedAt > loan.DueAtUtc)
            {
                loan.Status = LoanStatus.Late;
                var daysLate = Math.Ceiling((returnedAt - loan.DueAtUtc).TotalDays);
                loan.LateFee = (decimal)daysLate * 50m;
            }
            else
            {
                loan.Status = LoanStatus.Returned;
            }

            await _uow.Loans.UpdateAsync(loan, ct);
            await _uow.SaveChangesAsync(ct);

            var updated = await _uow.Loans.GetByIdAsync(id, ct);
            return _mapper.Map<LoanDto>(updated!);
        }

        // =====================================================================
        // RETUR: Admin
        // =====================================================================
        public async Task<LoanDto?> ReturnAsAdminAsync(
            Guid id,
            AdminLoanReturnDto dto,
            CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            if (loan is null) return null;

            if (loan.Status == LoanStatus.Returned || loan.Status == LoanStatus.Late)
                return _mapper.Map<LoanDto>(loan);

            var returnedAt = dto.ReturnedAtUtc;
            loan.ReturnedAtUtc = returnedAt;
            loan.Notes         = dto.Notes;

            if (returnedAt > loan.DueAtUtc)
            {
                loan.Status = LoanStatus.Late;
                var daysLate = Math.Ceiling((returnedAt - loan.DueAtUtc).TotalDays);
                loan.LateFee = (decimal)daysLate * 50m;
            }
            else
            {
                loan.Status = LoanStatus.Returned;
            }

            await _uow.Loans.UpdateAsync(loan, ct);
            await _uow.SaveChangesAsync(ct);

            var updated = await _uow.Loans.GetByIdAsync(id, ct);
            return _mapper.Map<LoanDto>(updated!);
        }

        // -------------------------------------------------
        // Sök (delegation till repo, mappning till DTO)
        // -------------------------------------------------
        public async Task<(IEnumerable<LoanDto> Items, int Total)> SearchAsync(
            Guid? memberId,
            Guid? toolId,
            int? status,
            bool openOnly,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            LoanStatus? statusEnum = status.HasValue
                ? (LoanStatus?)Enum.Parse(typeof(LoanStatus), status.Value.ToString())
                : null;

            var (items, total) = await _uow.Loans.SearchAsync(
                memberId, toolId, statusEnum, openOnly, page, pageSize, ct);

            var mapped = _mapper.Map<IEnumerable<LoanDto>>(items);
            return (mapped, total);
        }
    }
}
using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Loans;
using TooliRent.Services.Exceptions; // ToolUnavailableException
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services
{
    /// <summary>
    /// Domänlogik för lån:
    /// - Batch-utcheckning för medlemmar (MemberId tas från JWT i controller)
    /// - Batch-utcheckning för admin (MemberId anges per item)
    /// - Återlämning (separata flöden: medlem vs admin)
    /// - Sök och hämta enskilt lån
    ///
    /// Principer:
    /// - "Allt eller inget" i batch: validera alla poster först, skriv allt i ett svep.
    /// - Tider (CheckedOutAtUtc) sätts alltid på servern (UTC).
    /// - Vid lån från reservation: reservationen markeras Completed (arkiveras, ej soft delete).
    /// - Tillgänglighetskoll görs mot fönster [now, DueAtUtc] via ToolRepository.IsAvailableInWindowAsync.
    /// </summary>
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
        //  - MemberId injiceras via parametern currentMemberId (från JWT)
        //  - Allt eller inget: dubblettskydd och tillgänglighetskoll innan skrivning
        // =====================================================================
        public async Task<IEnumerable<LoanDto>> CheckoutBatchForMemberAsync(
            IEnumerable<LoanCheckoutDto> items,
            Guid currentMemberId,
            CancellationToken ct = default)
        {
            var now        = DateTime.UtcNow;
            var toCreate   = new List<Loan>();
            var toolsTaken = new HashSet<Guid>(); // skydd mot dubbletter i samma batch

            // 1) Validera samtliga poster (inget skrivs än)
            foreach (var it in items)
            {
                if (it.ReservationId is Guid rid)
                {
                    // ---- Lån baserat på reservation ----
                    var res = await _uow.Reservations.GetByIdAsync(rid, ct)
                              ?? throw new InvalidOperationException("Reservation not found.");

                    if (res.Status != ReservationStatus.Active)
                        throw new InvalidOperationException("Reservation is not active.");

                    if (res.MemberId != currentMemberId)
                        throw new UnauthorizedAccessException("Reservationen tillhör inte denna medlem.");

                    var tool = await _uow.Tools.GetByIdAsync(res.ToolId, ct)
                               ?? throw new InvalidOperationException("Tool not found.");

                    if (!toolsTaken.Add(res.ToolId))
                        throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                    if (now < res.StartUtc)
                        throw new InvalidOperationException($"Kan inte checka ut före {res.StartUtc:yyyy-MM-dd HH:mm} UTC.");
                    if (now >= res.EndUtc)
                        throw new InvalidOperationException("Reservationens sluttid har passerat.");

                    var dueAt = it.DueAtUtc ?? res.EndUtc;
                    if (dueAt <= now)
                        throw new InvalidOperationException("DueAtUtc/EndUtc måste vara i framtiden.");

                    // Ledighetskontroll i fönstret [nu, dueAt]
                    var free = await _uow.Tools.IsAvailableInWindowIgnoringAsync(
                        res.ToolId, now, dueAt,
                        ignoreReservationId: res.Id,
                        ignoreLoanId: null,
                        ct: ct);                    if (!free)
                        throw new ToolUnavailableException("Verktyget är inte tillgängligt i valt tidsintervall.");

                    toCreate.Add(new Loan
                    {
                        ToolId = tool.Id,
                        MemberId = currentMemberId,
                        ReservationId = res.Id,
                        CheckedOutAtUtc = now,
                        DueAtUtc = dueAt,
                        Status = LoanStatus.Open
                    });

                    // Markera reservationen som Completed (skrivs först vid SaveChanges)
                    res.Status = ReservationStatus.Completed;
                    await _uow.Reservations.UpdateAsync(res, ct);
                }
                else
                {
                    // ---- Direktlån (utan reservation) ----
                    var toolId = it.ToolId ?? throw new ArgumentException("ToolId krävs för direktlån.");
                    var dueAt  = it.DueAtUtc ?? throw new ArgumentException("DueAtUtc krävs för direktlån.");

                    var tool = await _uow.Tools.GetByIdAsync(toolId, ct)
                               ?? throw new InvalidOperationException("Tool not found.");

                    if (!toolsTaken.Add(toolId))
                        throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                    if (!tool.IsAvailable)
                        throw new ToolUnavailableException("Verktyget är markerat som otillgängligt.");

                    if (dueAt <= now)
                        throw new ArgumentException("DueAtUtc måste vara i framtiden.");

                    var free = await _uow.Tools.IsAvailableInWindowAsync(toolId, now, dueAt, ct);
                    if (!free)
                        throw new ToolUnavailableException("Verktyget är inte tillgängligt i valt tidsintervall.");

                    toCreate.Add(new Loan
                    {
                        ToolId = tool.Id,
                        MemberId = currentMemberId,
                        CheckedOutAtUtc = now,
                        DueAtUtc = dueAt,
                        Status = LoanStatus.Open
                    });
                }
            }

            // 2) Allt ser bra ut → skriv alla i ett svep
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
        // ADMIN: Batch-checkout (MemberId per item)
        //  - Varje item: antingen ReservationId ELLER (ToolId + MemberId + DueAtUtc)
        //  - Allt eller inget
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
                    // ---- Via reservation ----
                    var res = await _uow.Reservations.GetByIdAsync(rid, ct)
                              ?? throw new InvalidOperationException("Reservation not found.");
                    if (res.Status != ReservationStatus.Active)
                        throw new InvalidOperationException("Reservation is not active.");

                    var tool = await _uow.Tools.GetByIdAsync(res.ToolId, ct)
                               ?? throw new InvalidOperationException("Tool not found.");

                    if (!toolsTaken.Add(res.ToolId))
                        throw new InvalidOperationException("Samma verktyg förekommer flera gånger i batchen.");

                    if (now < res.StartUtc)
                        throw new InvalidOperationException($"Kan inte checka ut före {res.StartUtc:yyyy-MM-dd HH:mm} UTC.");
                    if (now >= res.EndUtc)
                        throw new InvalidOperationException("Reservationens sluttid har passerat.");

                    var dueAt = it.DueAtUtc ?? res.EndUtc;
                    if (dueAt <= now)
                        throw new InvalidOperationException("DueAtUtc/EndUtc måste vara i framtiden.");

                    var free = await _uow.Tools.IsAvailableInWindowAsync(res.ToolId, now, dueAt, ct);
                    if (!free)
                        throw new ToolUnavailableException("Verktyget är inte tillgängligt i valt tidsintervall.");

                    toCreate.Add(new Loan
                    {
                        ToolId = tool.Id,
                        MemberId = res.MemberId, // alltid från reservationen
                        ReservationId = res.Id,
                        CheckedOutAtUtc = now,
                        DueAtUtc = dueAt,
                        Status = LoanStatus.Open
                    });

                    res.Status = ReservationStatus.Completed;
                    await _uow.Reservations.UpdateAsync(res, ct);
                }
                else
                {
                    // ---- Direktlån ----
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
                        throw new ToolUnavailableException("Verktyget är markerat som otillgängligt.");

                    if (dueAt <= now)
                        throw new ArgumentException("DueAtUtc måste vara i framtiden.");

                    var free = await _uow.Tools.IsAvailableInWindowAsync(toolId, now, dueAt, ct);
                    if (!free)
                        throw new ToolUnavailableException("Verktyget är inte tillgängligt i valt tidsintervall.");

                    toCreate.Add(new Loan
                    {
                        ToolId = tool.Id,
                        MemberId = member.Id, // admin anger explicit
                        CheckedOutAtUtc = now,
                        DueAtUtc = dueAt,
                        Status = LoanStatus.Open
                    });
                }
            }

            // Spara alla i ett svep
            foreach (var l in toCreate)
                await _uow.Loans.AddAsync(l, ct);

            await _uow.SaveChangesAsync(ct);

            // Returnera (mappa)
            var result = new List<LoanDto>();
            foreach (var l in toCreate)
            {
                var fresh = await _uow.Loans.GetByIdAsync(l.Id, ct);
                result.Add(_mapper.Map<LoanDto>(fresh!));
            }
            return result;
        }

        // =====================================================================
        // RETUR: Medlem (sitt eget lån)
        //  - MemberId kommer från JWT (controller)
        //  - ReturnedAtUtc sätts alltid = UtcNow
        //  - Om lånet inte finns eller ej tillhör medlemmen → null (controller svarar 404)
        // =====================================================================
        public async Task<LoanDto?> ReturnAsMemberAsync(
            Guid id,
            Guid currentMemberId,
            LoanReturnDto dto,
            CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            if (loan is null) return null;

            // Ägarkontroll: exponera inte lånet om det inte tillhör medlemmen
            if (loan.MemberId != currentMemberId) return null;

            // Redan stängt? Returnera nuvarande status
            if (loan.Status == LoanStatus.Returned || loan.Status == LoanStatus.Late)
                return _mapper.Map<LoanDto>(loan);

            // Sätt returinfo
            var returnedAt = DateTime.UtcNow;
            loan.ReturnedAtUtc = returnedAt;
            loan.Notes = dto.Notes;

            // Status + ev. förseningsavgift
            if (returnedAt > loan.DueAtUtc)
            {
                loan.Status = LoanStatus.Late;
                var daysLate = Math.Ceiling((returnedAt - loan.DueAtUtc).TotalDays);
                loan.LateFee = (decimal)daysLate * 50m; // enkel modell
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
        //  - Admin anger ReturnedAtUtc själv (kan backdateras)
        //  - Om lånet inte finns → null (controller svarar 404)
        // =====================================================================
        public async Task<LoanDto?> ReturnAsAdminAsync(
            Guid id,
            AdminLoanReturnDto dto,
            CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            if (loan is null) return null;

            // Redan stängt? Returnera nuvarande status
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
        // Sök efter lån (admin / intern)
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
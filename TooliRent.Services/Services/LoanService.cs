// TooliRent.Services/Services/LoanService.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Loans;
using TooliRent.Services.Interfaces;
using TooliRent.Services.Exceptions; // ToolUnavailableException

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

        // ----------------------------
        // Hämta ett enskilt lån
        // ----------------------------
        public async Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            return loan is null ? null : _mapper.Map<LoanDto>(loan);
        }

        // ----------------------------
        // Direkt utlåning (utan reservation)
        // ----------------------------
        public async Task<LoanDto> CheckoutAsync(LoanCheckoutDto dto, CancellationToken ct = default)
        {
            // 1) Validera entiteter
            var tool = await _uow.Tools.GetByIdAsync(dto.ToolId, ct)
                       ?? throw new InvalidOperationException("Tool not found.");
            var member = await _uow.Members.GetByIdAsync(dto.MemberId, ct)
                         ?? throw new InvalidOperationException("Member not found.");

            // 2) Tider: due måste vara i framtiden
            var checkoutAt = DateTime.UtcNow;   // alltid serversatt
            if (dto.DueAtUtc <= checkoutAt)
                throw new ArgumentException("DueAtUtc must be after checkout time.", nameof(dto));

            // 3) Snabb flagg-koll (t.ex. trasigt/ur bruk)
            if (!tool.IsAvailable)
                throw new ToolUnavailableException("Verktyget är markerat som otillgängligt.");

            // 4) Fönsterkoll: verktyget måste vara ledigt (ingen annan reservation/lån som krockar)
            var free = await _uow.Tools.IsAvailableInWindowAsync(tool.Id, checkoutAt, dto.DueAtUtc, ct);
            if (!free)
                throw new ToolUnavailableException("Verktyget är inte tillgängligt i valt tidsintervall.");

            // 5) Skapa lån
            var loan = new Loan
            {
                ToolId = tool.Id,
                MemberId = member.Id,
                CheckedOutAtUtc = checkoutAt,
                DueAtUtc = dto.DueAtUtc,
                Status = LoanStatus.Open
            };

            await _uow.Loans.AddAsync(loan, ct);
            await _uow.SaveChangesAsync(ct);

            var created = await _uow.Loans.GetByIdAsync(loan.Id, ct);
            return _mapper.Map<LoanDto>(created!);
        }

        // ----------------------------
        // Utlåning baserat på en reservation
        // - Klienten skickar bara ReservationId
        // - CheckedOutAtUtc sätts ALLTID på servern
        // - Reservationen arkiveras (Status = Completed), ingen soft delete
        // ----------------------------
        public async Task<LoanDto> CheckoutFromReservationAsync(LoanCheckoutFromReservationDto dto, CancellationToken ct = default)
        {
            // 1) Hämta reservationen
            var res = await _uow.Reservations.GetByIdAsync(dto.ReservationId, ct)
                      ?? throw new InvalidOperationException("Reservation not found.");
            if (res.Status != ReservationStatus.Active)
                throw new InvalidOperationException("Reservation is not active.");

            // 2) Hämta verktyg och medlem
            var tool = await _uow.Tools.GetByIdAsync(res.ToolId, ct)
                       ?? throw new InvalidOperationException("Tool not found.");
            var member = await _uow.Members.GetByIdAsync(res.MemberId, ct)
                         ?? throw new InvalidOperationException("Member not found.");

            // 3) Checka ut endast inom reservationsfönstret
            var now = DateTime.UtcNow;
            if (now < res.StartUtc)
                throw new InvalidOperationException($"Du kan checka ut tidigast {res.StartUtc:yyyy-MM-dd HH:mm} UTC.");
            if (now >= res.EndUtc)
                throw new InvalidOperationException("Reservationen har redan passerat sitt slut.");

            // 4) Kolla om verktyget är utlånat just nu (krock med öppet lån)
            var loanConflict = await _uow.Loans.ToolIsLoanedNowAsync(res.ToolId, ct);
            if (loanConflict)
                throw new InvalidOperationException("Verktyget är redan utlånat just nu.");

            // 5) Bestäm lånetider: från nu till reservationens slut
            var checkoutAt = now;
            var dueAt = res.EndUtc;
            if (dueAt <= checkoutAt)
                throw new InvalidOperationException("Reservationens sluttid måste vara i framtiden.");

            // 6) Skapa lånet
            var loan = new Loan
            {
                ToolId = tool.Id,
                MemberId = member.Id,
                ReservationId = res.Id,
                CheckedOutAtUtc = checkoutAt,
                DueAtUtc = dueAt,
                Status = LoanStatus.Open
            };

            // 7) Arkivera reservationen (behåll i historik)
            res.Status = ReservationStatus.Completed;
            await _uow.Reservations.UpdateAsync(res, ct);

            // 8) Spara
            await _uow.Loans.AddAsync(loan, ct);
            await _uow.SaveChangesAsync(ct);

            var created = await _uow.Loans.GetByIdAsync(loan.Id, ct);
            return _mapper.Map<LoanDto>(created!);
        }

        // ----------------------------
        // Återlämna ett lån
        // ----------------------------
        public async Task<LoanDto?> ReturnAsync(Guid id, LoanReturnDto dto, CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            if (loan is null) return null;

            // Om redan återlämnad/låst → returnera direkt
            if (loan.Status == LoanStatus.Returned || loan.Status == LoanStatus.Late)
                return _mapper.Map<LoanDto>(loan);

            // 1) Markera som återlämnad
            loan.ReturnedAtUtc = dto.ReturnedAtUtc;
            loan.Notes = dto.Notes;

            // 2) Status + ev. förseningsavgift
            if (loan.ReturnedAtUtc.HasValue && loan.ReturnedAtUtc.Value > loan.DueAtUtc)
            {
                loan.Status = LoanStatus.Late;
                var daysLate = Math.Ceiling((loan.ReturnedAtUtc.Value - loan.DueAtUtc).TotalDays);
                loan.LateFee = (decimal)daysLate * 50m; // enkel modell: 50 kr/dag
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

        // ----------------------------
        // Sök efter lån (med filter)
        // ----------------------------
        public async Task<(IEnumerable<LoanDto> Items, int Total)> SearchAsync(
            Guid? memberId,
            Guid? toolId,
            int? status,
            bool openOnly,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            // Mappa status från int? till LoanStatus? enum
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
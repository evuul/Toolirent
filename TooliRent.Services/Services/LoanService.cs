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

        public async Task<LoanDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            return loan is null ? null : _mapper.Map<LoanDto>(loan);
        }

        public async Task<LoanDto> CheckoutAsync(LoanCheckoutDto dto, CancellationToken ct = default)
        {
            var tool = await _uow.Tools.GetByIdAsync(dto.ToolId, ct)
                       ?? throw new InvalidOperationException("Tool not found.");
            var member = await _uow.Members.GetByIdAsync(dto.MemberId, ct)
                         ?? throw new InvalidOperationException("Member not found.");

            if (dto.DueAtUtc <= DateTime.UtcNow)
                throw new ArgumentException("DueAtUtc must be in the future.", nameof(dto));

            var loan = new Loan
            {
                ToolId = tool.Id,
                MemberId = member.Id,
                CheckedOutAtUtc = DateTime.UtcNow,
                DueAtUtc = dto.DueAtUtc,
                Status = LoanStatus.Open
            };

            // Om en reservation angavs: koppla den och markera Completed
            if (dto.ReservationId is Guid rid)
            {
                var res = await _uow.Reservations.GetByIdAsync(rid, ct);
                if (res != null && res.Status == ReservationStatus.Active)
                {
                    res.Status = ReservationStatus.Completed;
                    await _uow.Reservations.UpdateAsync(res, ct);
                }

                // FK ägs av Loan → sätt ReservationId på lånet
                loan.ReservationId = rid;
            }

            await _uow.Loans.AddAsync(loan, ct);
            await _uow.SaveChangesAsync(ct);

            var created = await _uow.Loans.GetByIdAsync(loan.Id, ct);
            return _mapper.Map<LoanDto>(created!);
        }

        public async Task<LoanDto?> ReturnAsync(Guid id, LoanReturnDto dto, CancellationToken ct = default)
        {
            var loan = await _uow.Loans.GetByIdAsync(id, ct);
            if (loan is null) return null;

            // Om redan återlämnad, returnera aktuell status
            if (loan.Status == LoanStatus.Returned)
                return _mapper.Map<LoanDto>(loan);

            loan.ReturnedAtUtc = dto.ReturnedAtUtc;
            loan.Status = LoanStatus.Returned;
            loan.Notes = dto.Notes;

            // Enkel förseningsavgift: 50 kr/dag
            if (loan.ReturnedAtUtc.HasValue && loan.ReturnedAtUtc.Value > loan.DueAtUtc)
            {
                var daysLate = Math.Ceiling((loan.ReturnedAtUtc.Value - loan.DueAtUtc).TotalDays);
                loan.LateFee = (decimal)daysLate * 50m;
            }

            await _uow.Loans.UpdateAsync(loan, ct);
            await _uow.SaveChangesAsync(ct);

            var updated = await _uow.Loans.GetByIdAsync(loan.Id, ct);
            return _mapper.Map<LoanDto>(updated!);
        }

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
using AutoMapper;
using TooliRent.Core.Enums;
using TooliRent.Core.Interfaces;
using TooliRent.Core.Models;
using TooliRent.Services.DTOs.Loans;
using TooliRent.Services.Interfaces;

namespace TooliRent.Services.Services;

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
        var tool   = await _uow.Tools.GetByIdAsync(dto.ToolId, ct) 
                     ?? throw new InvalidOperationException("Tool not found.");
        var member = await _uow.Members.GetByIdAsync(dto.MemberId, ct) 
                     ?? throw new InvalidOperationException("Member not found.");

        if (dto.DueAtUtc <= DateTime.UtcNow)
            throw new ArgumentException("DueAtUtc must be in the future.", nameof(dto));

        // Skapa loan
        var loan = new Loan
        {
            ToolId = tool.Id,
            MemberId = member.Id,
            CheckedOutAtUtc = DateTime.UtcNow,
            DueAtUtc = dto.DueAtUtc,
            Status = LoanStatus.Open
        };

        // Om en reservation angavs: koppla den via Loan.ReservationId och markera reservationen som Completed
        if (dto.ReservationId is Guid rid)
        {
            var res = await _uow.Reservations.GetByIdAsync(rid, ct);
            if (res != null && res.Status == ReservationStatus.Active)
            {
                res.Status = ReservationStatus.Completed;
                await _uow.Reservations.UpdateAsync(res, ct);
            }

            loan.ReservationId = rid; // FK ägs av Loan
        }

        await _uow.Loans.AddAsync(loan, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.Loans.GetByIdAsync(loan.Id, ct);
        return _mapper.Map<LoanDto>(created!);
    }

    public async Task<LoanDto?> ReturnAsync(LoanReturnDto dto, CancellationToken ct = default)
    {
        var loan = await _uow.Loans.GetByIdAsync(dto.LoanId, ct);
        if (loan is null) return null;

        if (loan.Status == LoanStatus.Returned) return _mapper.Map<LoanDto>(loan);

        loan.ReturnedAtUtc = dto.ReturnedAtUtc;
        loan.Status = LoanStatus.Returned;
        loan.Notes = dto.Notes;

        // enkel sen avgift, om försenad
        if (loan.ReturnedAtUtc.HasValue && loan.ReturnedAtUtc.Value > loan.DueAtUtc)
        {
            var daysLate = Math.Ceiling((loan.ReturnedAtUtc.Value - loan.DueAtUtc).TotalDays);
            loan.LateFee = (decimal)daysLate * 50m; // exempel: 50 kr/dag
        }

        await _uow.Loans.UpdateAsync(loan, ct);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Loans.GetByIdAsync(loan.Id, ct);
        return _mapper.Map<LoanDto>(updated!);
    }
}
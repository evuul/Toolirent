// TooliRent.Services/Validators/Loans/LoanValidators.cs
using System;
using System.Linq;
using FluentValidation;
using TooliRent.Services.DTOs.Loans;

namespace TooliRent.Services.Validators.Loans
{
    // ========= MEMBER =========
    // Gäller LoanCheckoutDto (ReservationId ELLER ToolIds + DueAtUtc)
    public class LoanCheckoutDtoValidator : AbstractValidator<LoanCheckoutDto>
    {
        public LoanCheckoutDtoValidator()
        {
            RuleFor(x => x).Custom((dto, ctx) =>
            {
                var now = DateTime.UtcNow;

                if (dto.ReservationId.HasValue)
                {
                    // Via reservation: ToolIds ska inte skickas
                    if (dto.ToolIds != null && dto.ToolIds.Any())
                        ctx.AddFailure("ToolIds", "Ange inte ToolIds när ReservationId används.");

                    // DueAtUtc är valfri men om satt måste den vara i framtiden
                    if (dto.DueAtUtc.HasValue && dto.DueAtUtc.Value <= now)
                        ctx.AddFailure("DueAtUtc", "DueAtUtc måste vara i framtiden.");
                }
                else
                {
                    // Direktlån: minst ett verktyg + DueAtUtc krävs
                    if (dto.ToolIds == null || !dto.ToolIds.Any())
                        ctx.AddFailure("ToolIds", "Minst ett ToolId krävs vid direktlån.");

                    if (!dto.DueAtUtc.HasValue)
                        ctx.AddFailure("DueAtUtc", "DueAtUtc krävs vid direktlån.");
                    else if (dto.DueAtUtc.Value <= now)
                        ctx.AddFailure("DueAtUtc", "DueAtUtc måste vara i framtiden.");
                }
            });
        }
    }

    // (valfritt) om du validerar en hel batch i en POST-body som är en lista
    public class LoanCheckoutBatchValidator : AbstractValidator<IEnumerable<LoanCheckoutDto>>
    {
        public LoanCheckoutBatchValidator()
        {
            RuleForEach(x => x).SetValidator(new LoanCheckoutDtoValidator());

            // Förhindra dubblett-ToolIds i samma batch vid direktlån
            RuleFor(x => x).Custom((items, ctx) =>
            {
                var directToolIds = items
                    .Where(i => !i.ReservationId.HasValue && i.ToolIds != null)
                    .SelectMany(i => i.ToolIds!)
                    .ToList();

                var dupes = directToolIds
                    .GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (dupes.Count > 0)
                    ctx.AddFailure("ToolIds", $"Samma ToolId förekommer flera gånger i batchen: {string.Join(", ", dupes)}");
            });
        }
    }

    // ========= ADMIN =========
    // Antagande om din Admin-DTO:
    // public record AdminLoanCheckoutDto(Guid? ReservationId, Guid? ToolId, Guid? MemberId, DateTime? DueAtUtc);
    public class AdminLoanCheckoutDtoValidator : AbstractValidator<AdminLoanCheckoutDto>
    {
        public AdminLoanCheckoutDtoValidator()
        {
            RuleFor(x => x).Custom((dto, ctx) =>
            {
                var now = DateTime.UtcNow;

                if (dto.ReservationId.HasValue)
                {
                    // Via reservation: ToolId/MemberId ska inte skickas
                    if (dto.ToolId.HasValue)
                        ctx.AddFailure("ToolId", "Ange inte ToolId när ReservationId används.");
                    if (dto.MemberId.HasValue)
                        ctx.AddFailure("MemberId", "Ange inte MemberId när ReservationId används.");

                    if (dto.DueAtUtc.HasValue && dto.DueAtUtc.Value <= now)
                        ctx.AddFailure("DueAtUtc", "DueAtUtc måste vara i framtiden.");
                }
                else
                {
                    // Direktlån (single-item): ToolId, MemberId, DueAtUtc krävs
                    if (!dto.ToolId.HasValue)
                        ctx.AddFailure("ToolId", "ToolId krävs för direktlån.");
                    if (!dto.MemberId.HasValue)
                        ctx.AddFailure("MemberId", "MemberId krävs för direktlån.");
                    if (!dto.DueAtUtc.HasValue)
                        ctx.AddFailure("DueAtUtc", "DueAtUtc krävs för direktlån.");
                    else if (dto.DueAtUtc.Value <= now)
                        ctx.AddFailure("DueAtUtc", "DueAtUtc måste vara i framtiden.");
                }
            });
        }
    }

    public class AdminLoanCheckoutBatchValidator : AbstractValidator<IEnumerable<AdminLoanCheckoutDto>>
    {
        public AdminLoanCheckoutBatchValidator()
        {
            RuleForEach(x => x).SetValidator(new AdminLoanCheckoutDtoValidator());

            // Förhindra dubblett-ToolId vid direktlån i samma batch
            RuleFor(x => x).Custom((items, ctx) =>
            {
                var directToolIds = items
                    .Where(i => !i.ReservationId.HasValue && i.ToolId.HasValue)
                    .Select(i => i.ToolId!.Value)
                    .ToList();

                var dupes = directToolIds
                    .GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (dupes.Count > 0)
                    ctx.AddFailure("ToolId", $"Samma ToolId förekommer flera gånger i batchen: {string.Join(", ", dupes)}");
            });
        }
    }
}
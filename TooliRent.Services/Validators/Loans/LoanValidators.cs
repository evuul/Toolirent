using FluentValidation;
using TooliRent.Services.DTOs.Loans;

// ====================
// Medlem
// ====================
public class LoanCheckoutDtoValidator : AbstractValidator<LoanCheckoutDto>
{
    public LoanCheckoutDtoValidator()
    {
        // Om ReservationId finns → inga fler krav
        When(x => x.ReservationId.HasValue, () =>
        {
            RuleFor(x => x.ReservationId).NotEmpty();
        });

        // Annars krävs ToolId + DueAtUtc
        When(x => !x.ReservationId.HasValue, () =>
        {
            RuleFor(x => x.ToolId).NotEmpty().WithMessage("ToolId krävs för direktlån.");
            RuleFor(x => x.DueAtUtc)
                .NotNull().WithMessage("DueAtUtc krävs för direktlån.")
                .Must(d => d!.Value > DateTime.UtcNow).WithMessage("DueAtUtc måste vara i framtiden.");
        });
    }
}

// ====================
// Admin
// ====================
public class AdminLoanCheckoutDtoValidator : AbstractValidator<AdminLoanCheckoutDto>
{
    public AdminLoanCheckoutDtoValidator()
    {
        When(x => x.ReservationId.HasValue, () =>
        {
            RuleFor(x => x.ReservationId).NotEmpty();
        });

        When(x => !x.ReservationId.HasValue, () =>
        {
            RuleFor(x => x.ToolId).NotEmpty().WithMessage("ToolId krävs för direktlån.");
            RuleFor(x => x.DueAtUtc)
                .NotNull().WithMessage("DueAtUtc krävs för direktlån.")
                .Must(d => d!.Value > DateTime.UtcNow).WithMessage("DueAtUtc måste vara i framtiden.");
        });

        RuleFor(x => x.MemberId).NotEmpty().WithMessage("MemberId krävs (admin).");
    }
}
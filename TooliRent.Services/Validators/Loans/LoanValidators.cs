using System;
using FluentValidation;
using TooliRent.Services.DTOs.Loans;

// Direkt lån utan reservation
public class LoanCheckoutValidator : AbstractValidator<LoanCheckoutDto>
{
    public LoanCheckoutValidator()
    {
        RuleFor(x => x.ToolId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();

        RuleFor(x => x.DueAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Due date must be in the future.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
            .WithMessage("Due date cannot be more than 1 year from now.");
    }
}

// Lån baserat på reservation (endast ReservationId krävs)
public class LoanCheckoutFromReservationValidator : AbstractValidator<LoanCheckoutFromReservationDto>
{
    public LoanCheckoutFromReservationValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();

        // Om klienten skickar ett eget DueAtUtc:
        When(x => x.DueAtUtc.HasValue, () =>
        {
            RuleFor(x => x.DueAtUtc!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Due date must be in the future");
        });
    }
}

// Returnera ett lån (id kommer från route, så ingen LoanId i DTO)
public class LoanReturnDtoValidator : AbstractValidator<LoanReturnDto>
{
    public LoanReturnDtoValidator()
    {
        RuleFor(x => x.ReturnedAtUtc)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("ReturnedAtUtc cannot be in the distant future.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
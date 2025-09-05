using FluentValidation;
using TooliRent.Services.DTOs.Loans;

public class LoanCheckoutDtoValidator : AbstractValidator<LoanCheckoutDto>
{
    public LoanCheckoutDtoValidator()
    {
        RuleFor(x => x.ToolId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();

        RuleFor(x => x.DueAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Due date must be in the future")
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
            .WithMessage("Due date cannot be more than 1 year from now");

        RuleFor(x => x.ReservationId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("If ReservationId is provided, it must be a valid Guid");
    }
}

public class LoanReturnDtoValidator : AbstractValidator<LoanReturnDto>
{
    public LoanReturnDtoValidator()
    {
        RuleFor(x => x.LoanId).NotEmpty();

        RuleFor(x => x.ReturnedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("ReturnedAtUtc cannot be in the distant future");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters");
    }
}
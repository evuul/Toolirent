// TooliRent.Services/Validators/Loans/LoanValidators.cs
using FluentValidation;
using TooliRent.Services.DTOs.Loans;

public class LoanCheckoutDtoValidator : AbstractValidator<LoanCheckoutDto>
{
    public LoanCheckoutDtoValidator()
    {
        RuleFor(x => x.ToolId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.DueAtUtc).GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future");
        // ReservationId är optional
    }
}

public class LoanReturnDtoValidator : AbstractValidator<LoanReturnDto>
{
    public LoanReturnDtoValidator()
    {
        RuleFor(x => x.ReturnedAtUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("ReturnedAtUtc cannot be in the distant future");
    }
}
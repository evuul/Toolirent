// TooliRent.Services/Validators/Reservations/ReservationValidators.cs
using FluentValidation;
using TooliRent.Services.DTOs.Reservations;

public class ReservationCreateDtoValidator : AbstractValidator<ReservationCreateDto>
{
    public ReservationCreateDtoValidator()
    {
        RuleFor(x => x.ToolId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();

        RuleFor(x => x.StartUtc)
            .LessThan(x => x.EndUtc).WithMessage("StartUtc must be before EndUtc")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5)).WithMessage("StartUtc cannot be far in the past");

        RuleFor(x => x.EndUtc)
            .GreaterThan(DateTime.UtcNow).WithMessage("EndUtc must be in the future");
    }
}
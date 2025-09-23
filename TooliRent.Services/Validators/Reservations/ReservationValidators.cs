// TooliRent.Services/Validators/Reservations/ReservationValidators.cs
using FluentValidation;
using TooliRent.Services.DTOs.Reservations;

public class ReservationBatchCreateDtoValidator : AbstractValidator<ReservationCreateDto>
{
    public ReservationBatchCreateDtoValidator()
    {
        RuleFor(x => x.ToolIds)
            .NotNull().WithMessage("ToolIds krävs.")
            .Must(ids => ids.Any()).WithMessage("Minst ett verktyg måste väljas.")
            .Must(ids => ids.Distinct().Count() == ids.Count())
            .WithMessage("ToolIds innehåller dubletter.");

        // MemberId valideras inte som required – i /my/batch sätter controllern den från JWT.
        // Admin kan skicka MemberId i payload.

        RuleFor(x => x.StartUtc)
            .LessThan(x => x.EndUtc).WithMessage("StartUtc måste vara före EndUtc.")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5)).WithMessage("StartUtc kan inte vara långt bakåt i tiden.");

        RuleFor(x => x.EndUtc)
            .GreaterThan(DateTime.UtcNow).WithMessage("EndUtc måste ligga i framtiden.");
    }
}
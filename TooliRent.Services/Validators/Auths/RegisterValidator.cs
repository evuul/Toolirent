using FluentValidation;
using TooliRent.Services.DTOs.Auths;

namespace TooliRent.WebAPI.Validators.Auth;

public class RegisterValidator : AbstractValidator<AuthRegisterRequestDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email krävs.")
            .EmailAddress().WithMessage("Ogiltig emailadress.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lösenord krävs.")
            .MinimumLength(3).WithMessage("Lösenord måste vara minst 3 tecken.")
            .MaximumLength(50).WithMessage("Lösenord får max vara 50 tecken.") 
            .Matches("[A-Z]").WithMessage("Lösenord måste innehålla minst en versal.")
            .Matches("[a-z]").WithMessage("Lösenord måste innehålla minst en gemen.")
            .Matches("[0-9]").WithMessage("Lösenord måste innehålla minst en siffra.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Lösenord måste innehålla minst ett specialtecken.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Förnamn krävs.")
            .MaximumLength(50).WithMessage("Förnamn får vara max 50 tecken.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Efternamn krävs.")
            .MaximumLength(50).WithMessage("Efternamn får vara max 50 tecken.");
    }
}
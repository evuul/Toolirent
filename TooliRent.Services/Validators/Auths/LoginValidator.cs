using FluentValidation;
using TooliRent.Services.DTOs.Auths;

namespace TooliRent.WebAPI.Validators.Auth;

public class LoginValidator : AbstractValidator<AuthLoginRequestDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email krävs.")
            .EmailAddress().WithMessage("Ogiltig emailadress.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lösenord krävs.")
            .MaximumLength(256) // skyddar mot extremt långa payloads
            .Must(p => p == null || p.Trim().Length == p.Length)
            .WithMessage("Lösenord får inte börja eller sluta med mellanslag.");
    }
}
using FluentValidation;
using TooliRent.Services.DTOs.Auths;

namespace TooliRent.WebAPI.Validators.Auth;

public class RefreshRequestValidator : AbstractValidator<AuthRefreshRequestDto>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh Token krävs.")
            .MaximumLength(1000); // skyddar mot extremt långa payloads
    }
}
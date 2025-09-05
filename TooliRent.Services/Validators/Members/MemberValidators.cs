using FluentValidation;
using TooliRent.Services.DTOs.Members;

public class MemberCreateDtoValidator : AbstractValidator<MemberCreateDto>
{
    public MemberCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName). NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).    NotEmpty().EmailAddress().MaximumLength(200);
    }
}

public class MemberUpdateDtoValidator : AbstractValidator<MemberUpdateDto>
{
    public MemberUpdateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName). NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).    NotEmpty().EmailAddress().MaximumLength(200);
    }
}
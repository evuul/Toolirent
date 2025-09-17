using FluentValidation;
using TooliRent.Services.DTOs.Tools;

public class ToolCreateDtoValidator : AbstractValidator<ToolCreateDto>
{
    public ToolCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.RentalPricePerDay).GreaterThan(0).LessThanOrEqualTo(100000);
    }
}

public class ToolUpdateDtoValidator : AbstractValidator<ToolUpdateDto>
{
    public ToolUpdateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.RentalPricePerDay).GreaterThan(0).LessThanOrEqualTo(100000);
    }
}
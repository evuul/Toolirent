using FluentValidation;
using TooliRent.Services.DTOs.ToolCategories;

public class ToolCategoryCreateDtoValidator : AbstractValidator<ToolCategoryCreateDto>
{
    public ToolCategoryCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}

public class ToolCategoryUpdateDtoValidator : AbstractValidator<ToolCategoryUpdateDto>
{
    public ToolCategoryUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
    }
}
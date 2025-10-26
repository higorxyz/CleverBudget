using CleverBudget.Core.DTOs;
using FluentValidation;

namespace CleverBudget.Application.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome da categoria é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome não pode ter mais de 100 caracteres.");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("Ícone não pode ter mais de 50 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Icon));

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Cor deve estar no formato hexadecimal (#RRGGBB).")
            .When(x => !string.IsNullOrEmpty(x.Color));
    }
}
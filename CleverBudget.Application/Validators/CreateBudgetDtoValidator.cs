using CleverBudget.Core.DTOs;
using FluentValidation;

namespace CleverBudget.Application.Validators;

public class CreateBudgetDtoValidator : AbstractValidator<CreateBudgetDto>
{
    public CreateBudgetDtoValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Categoria é obrigatória");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("O valor do orçamento deve ser maior que zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("O valor do orçamento não pode ser maior que R$ 1.000.000");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("O mês deve estar entre 1 (Janeiro) e 12 (Dezembro)");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2020)
            .WithMessage("O ano deve ser 2020 ou posterior")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 5)
            .WithMessage($"O ano não pode ser maior que {DateTime.UtcNow.Year + 5}");
    }
}

public class UpdateBudgetDtoValidator : AbstractValidator<UpdateBudgetDto>
{
    public UpdateBudgetDtoValidator()
    {
        When(x => x.Amount.HasValue, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThan(0)
                .WithMessage("O valor do orçamento deve ser maior que zero")
                .LessThanOrEqualTo(1000000)
                .WithMessage("O valor do orçamento não pode ser maior que R$ 1.000.000");
        });
    }
}

using CleverBudget.Core.DTOs;
using FluentValidation;

namespace CleverBudget.Application.Validators;

public class CreateGoalDtoValidator : AbstractValidator<CreateGoalDto>
{
    public CreateGoalDtoValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Categoria é obrigatória.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0).WithMessage("Valor da meta deve ser maior que zero.")
            .LessThanOrEqualTo(1000000).WithMessage("Valor da meta não pode ser maior que 1.000.000.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Mês deve estar entre 1 e 12.");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(DateTime.Now.Year).WithMessage("Ano não pode ser no passado.")
            .LessThanOrEqualTo(DateTime.Now.Year + 10).WithMessage("Ano não pode ser maior que 10 anos no futuro.");
    }
}
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using FluentValidation;

namespace CleverBudget.Application.Validators;

public class CreateTransactionDtoValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero.")
            .LessThanOrEqualTo(1000000).WithMessage("Valor não pode ser maior que 1.000.000.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de transação inválido.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .MinimumLength(3).WithMessage("Descrição deve ter pelo menos 3 caracteres.")
            .MaximumLength(500).WithMessage("Descrição não pode ter mais de 500 caracteres.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Categoria é obrigatória.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Data é obrigatória.")
            .LessThanOrEqualTo(DateTime.Now.AddDays(1)).WithMessage("Data não pode ser no futuro.");
    }
}
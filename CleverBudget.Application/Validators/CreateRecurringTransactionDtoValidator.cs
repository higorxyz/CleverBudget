using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using FluentValidation;

namespace CleverBudget.Application.Validators;

public class CreateRecurringTransactionDtoValidator : AbstractValidator<CreateRecurringTransactionDto>
{
    public CreateRecurringTransactionDtoValidator()
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

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("Frequência inválida.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data de início é obrigatória.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("Data de término deve ser maior ou igual à data de início.");

        // Validação específica para frequência Mensal
        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31)
            .When(x => x.Frequency == RecurrenceFrequency.Monthly)
            .WithMessage("Dia do mês deve estar entre 1 e 31 para recorrências mensais.");

        RuleFor(x => x.DayOfMonth)
            .NotNull()
            .When(x => x.Frequency == RecurrenceFrequency.Monthly)
            .WithMessage("Dia do mês é obrigatório para recorrências mensais.");

        // Validação específica para frequência Semanal
        RuleFor(x => x.DayOfWeek)
            .NotNull()
            .When(x => x.Frequency == RecurrenceFrequency.Weekly)
            .WithMessage("Dia da semana é obrigatório para recorrências semanais.");
    }
}
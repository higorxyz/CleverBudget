using CleverBudget.Core.DTOs;
using FluentValidation;

namespace CleverBudget.Application.Validators;

public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .Length(2, 100).WithMessage("Nome deve ter entre 2 e 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Sobrenome é obrigatório")
            .Length(2, 100).WithMessage("Sobrenome deve ter entre 2 e 100 caracteres");
    }
}

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual é obrigatória");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches(@"[0-9]").WithMessage("Senha deve conter pelo menos um número");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.NewPassword).WithMessage("Senhas não conferem");
    }
}

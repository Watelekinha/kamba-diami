using FluentValidation;
using KambaDiami.Shared.Constants;

namespace KambaDiami.Application.Validators;

public static class PasswordRules
{
    public const int MinimumLength = 8;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("A palavra-passe é obrigatória.")
            .MinimumLength(MinimumLength).WithMessage($"A palavra-passe deve ter pelo menos {MinimumLength} caracteres.")
            .MaximumLength(128).WithMessage("A palavra-passe é demasiado longa.")
            .Matches("[A-Za-zÀ-ÿ]").WithMessage("A palavra-passe deve ter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A palavra-passe deve ter pelo menos um número.");

    public static IRuleBuilderOptions<T, string> AngolanMobile<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("O telefone é obrigatório.")
            .Must(AngolaPhone.IsValid).WithMessage("Indica um número móvel angolano válido (ex.: 923 456 789).");

    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Este campo é obrigatório.")
            .MaximumLength(100);
}

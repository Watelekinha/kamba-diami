using FluentValidation;
using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).PersonName();
        RuleFor(x => x.LastName).PersonName();
        RuleFor(x => x.PhoneNumber).AngolanMobile();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Indica um email válido.").MaximumLength(256);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("As palavras-passe não coincidem.");
        RuleFor(x => x.NeighborhoodId).GreaterThan(0).WithMessage("Morada é obrigatória.");
        RuleFor(x => x.StreetDetail).MaximumLength(200);
        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Familia or UserRole.Secretaria)
            .WithMessage("Tipo de conta inválido.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Indica um email válido.").MaximumLength(256);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Token).NotEmpty().WithMessage("A ligação de recuperação é inválida.");
        RuleFor(x => x.NewPassword).StrongPassword();
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("As palavras-passe não coincidem.");
    }
}

public class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.FirstName).PersonName();
        RuleFor(x => x.LastName).PersonName();
        RuleFor(x => x.PhoneNumber).AngolanMobile();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Indica um email válido.").MaximumLength(256);
        RuleFor(x => x.Password).StrongPassword();
    }
}

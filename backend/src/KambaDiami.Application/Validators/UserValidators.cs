using FluentValidation;
using KambaDiami.Application.Dtos;

namespace KambaDiami.Application.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).PersonName();
        RuleFor(x => x.LastName).PersonName();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Indica um email válido.").MaximumLength(256);
        RuleFor(x => x.PhoneNumber).AngolanMobile();
        RuleFor(x => x.MunicipalitySlug).NotEmpty().WithMessage("Selecciona a província e o município.");
        RuleFor(x => x.StreetDetail).MaximumLength(200);
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword();
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("As palavras-passe não coincidem.");
    }
}

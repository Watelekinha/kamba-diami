using FluentValidation;
using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Enums;
using KambaDiami.Shared.Constants;

namespace KambaDiami.Application.Validators;

public class CreateJobPostingRequestValidator : AbstractValidator<CreateJobPostingRequest>
{
    public CreateJobPostingRequestValidator()
    {
        RuleFor(x => x.CategoryIds).NotEmpty().WithMessage("Selecciona pelo menos uma área/categoria.");
        RuleFor(x => x.MunicipalitySlugs).NotEmpty().WithMessage("Selecciona pelo menos um município.");
        RuleFor(x => x.MunicipalitySlugs.Count).LessThanOrEqualTo(20).WithMessage("Indica no máximo 20 municípios.");
        RuleFor(x => x.PaymentValue).GreaterThan(0).WithMessage("O valor de pagamento deve ser maior que zero.");
        RuleFor(x => x.PaymentValue)
            .GreaterThanOrEqualTo(ContractConstants.MinimumMonthlyValue)
            .When(x => x.PaymentType == PaymentType.Mensal)
            .WithMessage($"Para contratos mensais, o valor mínimo é {ContractConstants.MinimumMonthlyValue:N0} Kzs.");
        RuleFor(x => x.HoursPerDay)
            .GreaterThan(0)
            .When(x => x.HoursPerDay.HasValue)
            .WithMessage("As horas por dia devem ser maiores que zero.");
    }
}

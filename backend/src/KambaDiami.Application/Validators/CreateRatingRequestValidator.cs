using FluentValidation;
using KambaDiami.Application.Dtos;

namespace KambaDiami.Application.Validators;

public class CreateRatingRequestValidator : AbstractValidator<CreateRatingRequest>
{
    public CreateRatingRequestValidator()
    {
        RuleFor(x => x.Score)
            .InclusiveBetween(1, 5)
            .WithMessage("A avaliação deve ser entre 1 e 5 estrelas.");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment))
            .WithMessage("O comentário não pode exceder 500 caracteres.");
    }
}

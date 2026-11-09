using FluentValidation;
using KambaDiami.Application.Dtos;

namespace KambaDiami.Application.Validators;

public class UpdateContractClausesRequestValidator : AbstractValidator<UpdateContractClausesRequest>
{
    public UpdateContractClausesRequestValidator()
    {
        RuleFor(x => x.Clauses)
            .NotEmpty()
            .WithMessage("O contrato deve ter pelo menos uma cláusula.");

        RuleForEach(x => x.Clauses).ChildRules(clause =>
        {
            clause.RuleFor(c => c.Title)
                .NotEmpty().WithMessage("Cada cláusula precisa de um título.")
                .MaximumLength(200).WithMessage("O título da cláusula não pode exceder 200 caracteres.");

            clause.RuleFor(c => c.Body)
                .NotEmpty().WithMessage("Cada cláusula precisa de um texto.");
        });
    }
}

using FluentValidation;

namespace IOCv2.Application.Features.Evaluations.Commands.SaveEvaluations;

internal class SaveEvaluationsValidator : AbstractValidator<SaveEvaluationsCommand>
{
    public SaveEvaluationsValidator()
    {
        RuleFor(x => x.InternshipId)
            .NotEmpty().WithMessage("InternshipId is required.");

        RuleFor(x => x.Evaluations)
            .NotNull()
            .NotEmpty().WithMessage("At least one evaluation must be provided.");

        RuleForEach(x => x.Evaluations).ChildRules(eval =>
        {
            eval.RuleFor(e => e.Details)
                .NotNull();

            eval.RuleForEach(e => e.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.CriteriaId)
                    .NotEmpty().WithMessage("CriteriaId is required.");

                detail.RuleFor(d => d.Score)
                    .GreaterThanOrEqualTo(0).WithMessage("Score must be >= 0.");
            });
        });
    }
}

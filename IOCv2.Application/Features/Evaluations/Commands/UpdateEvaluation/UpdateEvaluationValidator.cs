using FluentValidation;

namespace IOCv2.Application.Features.Evaluations.Commands.UpdateEvaluation;

internal class UpdateEvaluationValidator : AbstractValidator<UpdateEvaluationCommand>
{
    public UpdateEvaluationValidator()
    {
        RuleFor(x => x.EvaluationId)
            .NotEmpty().WithMessage("EvaluationId is required.");

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.CriteriaId)
                .NotEmpty().WithMessage("CriteriaId is required.");

            detail.RuleFor(d => d.Score)
                .GreaterThanOrEqualTo(0).WithMessage("Score must be >= 0.");
        });
    }
}

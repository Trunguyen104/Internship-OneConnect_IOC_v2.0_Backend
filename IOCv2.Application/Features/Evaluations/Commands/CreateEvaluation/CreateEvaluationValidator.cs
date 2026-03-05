using FluentValidation;

namespace IOCv2.Application.Features.Evaluations.Commands.CreateEvaluation;

internal class CreateEvaluationValidator : AbstractValidator<CreateEvaluationCommand>
{
    public CreateEvaluationValidator()
    {
        RuleFor(x => x.InternshipId)
            .NotEmpty().WithMessage("InternshipId is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required.");

        RuleFor(x => x.Details)
            .NotNull();

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.CriteriaId)
                .NotEmpty().WithMessage("CriteriaId is required.");

            detail.RuleFor(d => d.Score)
                .GreaterThanOrEqualTo(0).WithMessage("Score must be >= 0.");
        });
    }
}

using FluentValidation;

namespace IOCv2.Application.Features.Evaluations.Queries.GetInternshipEvaluations;

internal class GetInternshipEvaluationsValidator : AbstractValidator<GetInternshipEvaluationsQuery>
{
    public GetInternshipEvaluationsValidator()
    {
        RuleFor(x => x.CycleId)
            .NotEmpty().WithMessage("CycleId is required.");

        RuleFor(x => x.InternshipId)
            .NotEmpty().WithMessage("InternshipId is required.");
    }
}

using FluentValidation;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    internal class GetStakeholdersValidator : AbstractValidator<GetStakeholdersQuery>
    {
        public GetStakeholdersValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page size must be at least 1.")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size cannot exceed 100.");
        }
    }
}


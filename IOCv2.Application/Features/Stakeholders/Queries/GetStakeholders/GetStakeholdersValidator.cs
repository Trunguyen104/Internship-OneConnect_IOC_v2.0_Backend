using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    internal class GetStakeholdersValidator : AbstractValidator<GetStakeholdersQuery>
    {
        public GetStakeholdersValidator()
        {
            RuleFor(x => x.InternshipId)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.ProjectIdRequired);

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(MessageKeys.Common.PageNumberInvalid);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage(MessageKeys.Common.PageSizeInvalid)
                .LessThanOrEqualTo(100)
                .WithMessage(MessageKeys.Common.PageSizeTooLarge);
        }
    }
}


using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    internal class GetStakeholderByIdValidator : AbstractValidator<GetStakeholderByIdQuery>
    {
        public GetStakeholderByIdValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.IdRequired);
        }
    }
}


using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhases;

public class GetInternshipPhasesValidator : AbstractValidator<GetInternshipPhasesQuery>
{
    public GetInternshipPhasesValidator(IMessageService messageService)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.PageNumberMinValue));

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.PageSizeRange));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StatusInvalid))
            .When(x => x.Status.HasValue);
    }
}

using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;

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
            .Must(status => string.IsNullOrWhiteSpace(status)
                || status.Equals("Upcoming", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Ended", StringComparison.OrdinalIgnoreCase))
            .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StatusInvalid));
    }
}

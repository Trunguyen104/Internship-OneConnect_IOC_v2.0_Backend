using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;

public class UpdateInternshipPhaseValidator : AbstractValidator<UpdateInternshipPhaseCommand>
{
    public UpdateInternshipPhaseValidator(IMessageService messageService)
    {
        RuleFor(x => x.PhaseId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.PhaseIdRequired));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.NameRequired))
            .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.NameMaxLength));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StartDateRequired))
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StartDateNotInPast));

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.EndDateRequired))
            .GreaterThan(x => x.StartDate)
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.EndDateAfterStartDate))
            .Must((x, endDate) => (endDate.DayNumber - x.StartDate.DayNumber) >= 28)
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.DurationTooShort))
            .Must((x, endDate) => (endDate.DayNumber - x.StartDate.DayNumber) <= 365)
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.DurationTooLong));

        RuleFor(x => x.MajorFields)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.MajorFieldsRequired));

        RuleFor(x => x.Capacity)
            .GreaterThanOrEqualTo(1).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.CapacityMinValue));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.DescriptionMaxLength))
            .When(x => x.Description != null);
    }
}

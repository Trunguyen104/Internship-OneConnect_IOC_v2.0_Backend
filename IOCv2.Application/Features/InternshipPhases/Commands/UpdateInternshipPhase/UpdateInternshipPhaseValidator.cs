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
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StartDateRequired));

        // BUG-B FIX: Add StartDate past-guard (was only in CreateValidator, not UpdateValidator).
        // Without this, a Draft phase could have its StartDate backdated, creating audit trail inconsistencies.
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StartDateNotInPast))
            .When(x => x.StartDate != default);

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.EndDateRequired))
            .GreaterThan(x => x.StartDate)
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.EndDateAfterStartDate));

        RuleFor(x => x.MaxStudents)
            .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.MaxStudentsGreaterThanZero))
            .When(x => x.MaxStudents.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StatusInvalid));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.DescriptionMaxLength))
            .When(x => x.Description != null);
    }
}

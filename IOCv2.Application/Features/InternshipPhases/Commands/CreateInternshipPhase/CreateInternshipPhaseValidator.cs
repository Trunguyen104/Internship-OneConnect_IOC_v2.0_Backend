using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipPhases.Commands.CreateInternshipPhase;

public class CreateInternshipPhaseValidator : AbstractValidator<CreateInternshipPhaseCommand>
{
    public CreateInternshipPhaseValidator(IMessageService messageService)
    {
        RuleFor(x => x.EnterpriseId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseIdRequired));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.NameRequired))
            .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.NameMaxLength));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.StartDateRequired));

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

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.DescriptionMaxLength))
            .When(x => x.Description != null);
    }
}

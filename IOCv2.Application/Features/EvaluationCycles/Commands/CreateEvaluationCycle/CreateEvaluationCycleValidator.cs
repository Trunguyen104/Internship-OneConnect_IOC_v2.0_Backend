using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;

public class CreateEvaluationCycleValidator : AbstractValidator<CreateEvaluationCycleCommand>
{
    public CreateEvaluationCycleValidator(IMessageService messageService)
    {
        RuleFor(x => x.PhaseId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.NameRequired))
            .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.NameMaxLength));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.StartDateRequired));

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.EndDateRequired))
            .GreaterThan(x => x.StartDate)
            .WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.EndDateMustBeAfterStart));
    }
}

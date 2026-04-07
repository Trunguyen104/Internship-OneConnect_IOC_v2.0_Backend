using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public class UpdateEvaluationCycleValidator : AbstractValidator<UpdateEvaluationCycleCommand>
{
    public UpdateEvaluationCycleValidator(IMessageService messageService)
    {
        RuleFor(x => x.CycleId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.NameRequired))
            .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.NameMaxLength));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.StartDateRequired));

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.EndDateRequired))
            .Must((cmd, endDate) => endDate.Date > cmd.StartDate.Date)
            .WithMessage(messageService.GetMessage(MessageKeys.EvaluationCycle.EndDateMustBeAfterStart));

        RuleFor(x => x.Status)
            .IsInEnum();

    }
}

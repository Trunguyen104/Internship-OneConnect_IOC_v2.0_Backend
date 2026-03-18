using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.UpdateEvaluationCriteria;

public class UpdateEvaluationCriteriaValidator : AbstractValidator<UpdateEvaluationCriteriaCommand>
{
    public UpdateEvaluationCriteriaValidator(IMessageService messageService)
    {
        RuleFor(x => x.CriteriaId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.NameRequired))
            .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.NameMaxLength));

        RuleFor(x => x.MaxScore)
            .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.MaxScoreInvalid))
            .LessThanOrEqualTo(100);

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.WeightInvalid))
            .LessThanOrEqualTo(100);
    }
}

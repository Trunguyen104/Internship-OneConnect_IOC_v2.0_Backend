using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

internal class UpdateWorkItemValidator : AbstractValidator<UpdateWorkItemCommand>
{

    public UpdateWorkItemValidator(IMessageService messageService)
    {
        RuleFor(x => x.Title)
            .MaximumLength(255)
            .When(x => x.Title != null)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.TitleMaxLength));

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.PriorityInvalid));

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.StatusInvalid));

        RuleFor(x => x.StoryPoint)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StoryPoint.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.StoryPointInvalid));
    }
}

using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

internal class UpdateWorkItemValidator : AbstractValidator<UpdateWorkItemCommand>
{
    private static readonly string[] ValidPriorities = Enum.GetNames<Priority>();
    private static readonly string[] ValidStatuses = Enum.GetNames<WorkItemStatus>();

    public UpdateWorkItemValidator(IMessageService messageService)
    {
        RuleFor(x => x.Title)
            .MaximumLength(255)
            .When(x => x.Title != null)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.TitleMaxLength));

        RuleFor(x => x.Priority)
            .Must(p => p == null || ValidPriorities.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.PriorityInvalid));

        RuleFor(x => x.Status)
            .Must(s => s == null || ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.StatusInvalid));

        RuleFor(x => x.StoryPoint)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StoryPoint.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.StoryPointInvalid));
    }
}

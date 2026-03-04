using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;

internal class CreateWorkItemValidator : AbstractValidator<CreateWorkItemCommand>
{
    private static readonly string[] ValidTypes = Enum.GetNames<WorkItemType>();
    private static readonly string[] ValidPriorities = Enum.GetNames<Priority>();

    public CreateWorkItemValidator(IMessageService messageService)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.TitleRequired))
            .MaximumLength(255)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.TitleMaxLength));

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.TypeRequired))
            .Must(t => ValidTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.TypeInvalid));

        RuleFor(x => x.Priority)
            .Must(p => p == null || ValidPriorities.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.PriorityInvalid));

        RuleFor(x => x.StoryPoint)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StoryPoint.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.WorkItem.StoryPointInvalid));
    }
}

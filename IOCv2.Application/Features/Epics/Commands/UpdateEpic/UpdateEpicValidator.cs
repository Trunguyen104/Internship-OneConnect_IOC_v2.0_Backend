using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

internal class UpdateEpicValidator : AbstractValidator<UpdateEpicCommand>
{
    public UpdateEpicValidator(IMessageService messageService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Epic.NameRequired))
            .MaximumLength(255)
            .WithMessage(messageService.GetMessage(MessageKeys.Epic.NameMaxLength));

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage(messageService.GetMessage(MessageKeys.Epic.DescriptionMaxLength));
    }
}

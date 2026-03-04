using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicValidator : AbstractValidator<DeleteEpicCommand>
{
    public DeleteEpicValidator(IMessageService messageService)
    {
        RuleFor(x => x.EpicId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Epic.EpicIdRequired));
    }
}

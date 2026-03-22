using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Enterprises.Commands.AcceptApplication;

public class AcceptApplicationCommandValidator : AbstractValidator<AcceptApplicationCommand>
{
    public AcceptApplicationCommandValidator(IMessageService messageService)
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ApplicationIdRequired));
    }
}

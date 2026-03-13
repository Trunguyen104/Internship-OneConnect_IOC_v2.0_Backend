using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Enterprises.Commands.RejectApplication;

public class RejectApplicationCommandValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationCommandValidator(IMessageService messageService)
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ApplicationIdRequired));

        RuleFor(x => x.RejectReason)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.RejectReasonRequired))
            .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.RejectReasonMaxLength));
    }
}

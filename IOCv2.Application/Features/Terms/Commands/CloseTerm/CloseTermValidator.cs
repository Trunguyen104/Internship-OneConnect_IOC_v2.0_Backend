using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Terms.Commands.CloseTerm;

public class CloseTermValidator : AbstractValidator<CloseTermCommand>
{
    public CloseTermValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest));

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest));
    }
}
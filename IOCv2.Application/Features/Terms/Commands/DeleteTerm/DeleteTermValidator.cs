using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Terms.Commands.DeleteTerm;

public class DeleteTermValidator : AbstractValidator<DeleteTermCommand>
{
    public DeleteTermValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest));
    }
}
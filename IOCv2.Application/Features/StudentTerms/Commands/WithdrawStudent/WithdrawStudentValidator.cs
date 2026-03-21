using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public class WithdrawStudentValidator : AbstractValidator<WithdrawStudentCommand>
{
    public WithdrawStudentValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentTermIdRequired));
    }
}

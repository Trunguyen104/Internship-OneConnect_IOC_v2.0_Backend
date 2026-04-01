using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.StudentApplications.Commands.WithdrawApplication;

internal class WithdrawApplicationValidator : AbstractValidator<WithdrawApplicationCommand>
{
    public WithdrawApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(MessageKeys.Validation.IdRequired);
    }
}

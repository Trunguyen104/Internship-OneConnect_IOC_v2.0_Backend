using IOCv2.Application.Constants;
using FluentValidation;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.RejectApplication;

internal class RejectApplicationValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage(MessageKeys.HRApplications.ApplicationIdRequired);

        RuleFor(x => x.RejectReason)
            .NotEmpty()
            .WithMessage(MessageKeys.HRApplications.RejectReasonRequired)
            .MinimumLength(10)
            .WithMessage(MessageKeys.HRApplications.RejectReasonMinLength)
            .MaximumLength(1000)
            .WithMessage(MessageKeys.HRApplications.RejectReasonMaxLength);
    }
}

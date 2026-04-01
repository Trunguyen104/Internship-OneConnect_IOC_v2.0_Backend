using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.StudentApplications.Commands.HideApplication;

internal class HideApplicationValidator : AbstractValidator<HideApplicationCommand>
{
    public HideApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(MessageKeys.Validation.IdRequired);
    }
}

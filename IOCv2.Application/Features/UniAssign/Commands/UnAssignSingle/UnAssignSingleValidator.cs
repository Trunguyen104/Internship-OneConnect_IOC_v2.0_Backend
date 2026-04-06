using System;
using FluentValidation;

namespace IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle
{
    public class UnAssignSingleValidator : AbstractValidator<UnAssignSingleCommand>
    {
        public UnAssignSingleValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty()
                .WithMessage("ApplicationId is required.");

            RuleFor(x => x.ApplicationId)
                .NotEqual(Guid.Empty)
                .WithMessage("ApplicationId must not be an empty GUID.");
        }
    }
}

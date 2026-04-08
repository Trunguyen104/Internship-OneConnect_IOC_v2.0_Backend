using System;
using FluentValidation;

namespace IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle
{
    public class UnAssignSingleValidator : AbstractValidator<UnAssignSingleCommand>
    {
        public UnAssignSingleValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty()
                .WithMessage("StudentId is required.");

            RuleFor(x => x.StudentId)
                .NotEqual(Guid.Empty)
                .WithMessage("StudentId must not be an empty GUID.");
        }
    }
}

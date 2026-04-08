using FluentValidation;
using System;

namespace IOCv2.Application.Features.UniAssign.Commands.ReAssignSingle
{
    public class ReAssignSingleValidator : AbstractValidator<ReAssignSingleCommand>
    {
        public ReAssignSingleValidator()
        {
            RuleFor(x => x.StudentId)
                .Must(id => id != Guid.Empty)
                .WithMessage("StudentId must be a non-empty GUID.");

            RuleFor(x => x.NewEnterpriseId)
                .Must(id => id != Guid.Empty)
                .WithMessage("NewEnterpriseId must be a non-empty GUID.");

            RuleFor(x => x.NewInternPhaseId)
                .Must(id => id != Guid.Empty)
                .WithMessage("NewInternPhaseId must be a non-empty GUID.");
        }
    }
}
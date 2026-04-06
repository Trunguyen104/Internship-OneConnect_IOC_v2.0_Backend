using FluentValidation;
using System;
using System.Linq;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkReassignEnterprise
{
    public class BulkReassignEnterpriseValidator : AbstractValidator<BulkReassignEnterpriseCommand>
    {
        public BulkReassignEnterpriseValidator()
        {
            RuleFor(x => x.NewEnterpriseId)
                .NotEmpty()
                .WithMessage("NewEnterpriseId must be provided.");

            RuleFor(x => x.NewInternPhaseId)
                .NotEmpty()
                .WithMessage("NewInternPhaseId must be provided.");

            RuleFor(x => x.StudentIds)
                .NotNull()
                .WithMessage("StudentIds collection must be provided.")
                .Must(list => list != null && list.Any())
                .WithMessage("At least one student must be selected.")
                .Must(list => list == null || list.Distinct().Count() == list.Count)
                .WithMessage("Duplicate student IDs are not allowed.");

            RuleForEach(x => x.StudentIds)
                .NotEmpty()
                .WithMessage("StudentId must not be empty.")
                .Must(id => id != Guid.Empty)
                .WithMessage("StudentId must be a valid GUID.");
        }
    }
}

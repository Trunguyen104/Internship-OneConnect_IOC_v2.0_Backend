using FluentValidation;
using System;
using System.Linq;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkUnassign
{
    public class BulkUnassignValidator : AbstractValidator<BulkUnassignCommand>
    {
        public BulkUnassignValidator()
        {
            RuleFor(x => x.StudentIds)
                .NotNull().WithMessage("StudentIds must be provided.")
                .Must(list => list != null && list.Any()).WithMessage("At least one student must be selected.")
                .Must(list => list == null || list.Count <= 1000)
                    .WithMessage("Cannot unassign more than 1000 students at once.");

            RuleForEach(x => x.StudentIds)
                .NotEqual(Guid.Empty).WithMessage("Each StudentId must be a valid GUID.");
        }
    }
}

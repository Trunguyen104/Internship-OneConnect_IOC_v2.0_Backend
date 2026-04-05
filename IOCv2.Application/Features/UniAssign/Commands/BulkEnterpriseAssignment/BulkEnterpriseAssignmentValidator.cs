using FluentValidation;
using System;
using System.Linq;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment
{
    public class BulkEnterpriseAssignmentValidator : AbstractValidator<BulkEnterpriseAssignmentCommand>
    {
        public BulkEnterpriseAssignmentValidator()
        {
            RuleFor(x => x.EnterpriseId)
                .NotEmpty()
                .WithMessage("EnterpriseId is required.");

            RuleFor(x => x.InternPhaseId)
                .NotEmpty()
                .WithMessage("InternPhaseId is required.");

            RuleFor(x => x.StudentIds)
                .NotNull()
                .WithMessage("StudentIds is required.")
                .Must(list => list.Any())
                .WithMessage("At least one StudentId must be provided.")
                .Must(list => list.All(id => id != Guid.Empty))
                .WithMessage("StudentIds contains an invalid Guid (empty).")
                .Must(list => list.Distinct().Count() == list.Count)
                .WithMessage("StudentIds contains duplicate ids.")
                .Must(list => list.Count <= 500)
                .WithMessage("StudentIds cannot contain more than 500 items.");
        }
    }
}

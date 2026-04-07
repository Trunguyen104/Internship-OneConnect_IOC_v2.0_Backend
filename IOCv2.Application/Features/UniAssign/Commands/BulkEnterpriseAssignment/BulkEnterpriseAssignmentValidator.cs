using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Linq;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment
{
    public class BulkEnterpriseAssignmentValidator : AbstractValidator<BulkEnterpriseAssignmentCommand>
    {
        private readonly IMessageService _messageService;
        public BulkEnterpriseAssignmentValidator(IMessageService messageService)
        {
            _messageService = messageService;
            RuleFor(x => x.EnterpriseId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.UniAssign.EnterpriseIdIsRequired));

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

using FluentValidation;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public class UpdateInternshipApplicationStatusValidator : AbstractValidator<UpdateInternshipApplicationStatusCommand>
    {
        public UpdateInternshipApplicationStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage(messageService.GetMessage("Application.StatusInvalid"));

            RuleFor(x => x.RejectReason)
                .NotEmpty()
                .When(x => x.NewStatus == InternshipApplicationStatus.Rejected)
                .WithMessage(messageService.GetMessage("Application.RejectReasonRequired"));

            RuleFor(x => x.RejectReason)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.RejectReason))
                .WithMessage(messageService.GetMessage("Validation.DescriptionMaxLength"));
        }
    }
}

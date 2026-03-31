using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    public class UpdateJobValidator : AbstractValidator<UpdateJobCommand>
    {
        public UpdateJobValidator(IMessageService messageService)
        {
            When(x => x.Status == JobStatus.PUBLISHED, () =>
            {
                // Title and basic text fields
                RuleFor(x => x.Title)
                    .NotEmpty()
                    .WithMessage(messageService.GetMessage("Title is required."));

                RuleFor(x => x.Description)
                    .MaximumLength(4000)
                    .WithMessage(messageService.GetMessage("Description is too long."));

                RuleFor(x => x.Requirements)
                    .MaximumLength(4000)
                    .WithMessage(messageService.GetMessage("Requirements is too long."));

                RuleFor(x => x.Benefit)
                    .MaximumLength(2000)
                    .WithMessage(messageService.GetMessage("Benefit is too long."));

                RuleFor(x => x.Location)
                    .NotEmpty()
                    .MaximumLength(255)
                    .WithMessage(messageService.GetMessage("Location is too long."));

                RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .When(x => x.Quantity.HasValue)
                    .WithMessage(messageService.GetMessage("Quantity must be 0 or greater."));

                // ExpireDate (if provided) must be today or in the future
                RuleFor(x => x.ExpireDate)
                    .Must(date => date == null || date.Value.Date >= DateTime.UtcNow.Date)
                    .WithMessage(messageService.GetMessage("Expire date must be today or later."));

                // Audience rules
                RuleFor(x => x.Audience)
                    .IsInEnum()
                    .WithMessage(messageService.GetMessage("Invalid audience."));

                When(x => x.Audience == JobAudience.Targeted, () =>
                {
                    RuleFor(x => x.UniversityIds)
                        .NotEmpty()
                        .Must(list => list != null && list.Count == 1)
                        .WithMessage(messageService.GetMessage("University must be provided for targeted audience."));
                });
            });

            When(x => x.Status == JobStatus.DRAFT, () =>
            {
                RuleFor(x => x.StartDate)
                    .Must(d => !d.HasValue)
                    .WithMessage("StartDate is read-only and inherited from InternshipPhase.");

                RuleFor(x => x.EndDate)
                    .Must(d => !d.HasValue)
                    .WithMessage("EndDate is read-only and inherited from InternshipPhase.");

                RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .When(x => x.Quantity.HasValue)
                    .WithMessage(messageService.GetMessage("Quantity must be 0 or greater."));
            });

            RuleFor(x => x.InternshipPhaseId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("Intern phase id is invalid.");
        }
    }
}

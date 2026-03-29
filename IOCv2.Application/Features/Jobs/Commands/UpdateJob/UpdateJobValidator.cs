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

                // Internship period: required and valid
                RuleFor(x => x.StartDate)
                    .NotEmpty()
                    .WithMessage(messageService.GetMessage("Start date is required."))
                    .Must(d => d.HasValue && d.Value.Date >= DateTime.UtcNow.Date)
                    .WithMessage(messageService.GetMessage("Start date must be today or later."));

                RuleFor(x => x.EndDate)
                    .NotEmpty()
                    .WithMessage(messageService.GetMessage("End date is required."))
                    .Must((cmd, end) => end.HasValue && cmd.StartDate.HasValue && end.Value > cmd.StartDate.Value.AddDays(JobsPostingParam.Common.MinimumDurationDays))
                    .WithMessage(messageService.GetMessage($"End date must be at least {JobsPostingParam.Common.MinimumDurationDays} days after start date."))
                    .Must((cmd, end) => end.HasValue && cmd.StartDate.HasValue && end.Value <= cmd.StartDate.Value.AddDays(JobsPostingParam.Common.MaximumDurationDays))
                    .WithMessage(messageService.GetMessage($"End date must be at most {JobsPostingParam.Common.MaximumDurationDays} days after start date."));

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
                    .Must(d => d == null || d.Value.Date >= DateTime.UtcNow.Date)
                    .WithMessage(messageService.GetMessage("Start date must be today or later."));

                RuleFor(x => x.EndDate)
                    .Must((cmd, end) => !end.HasValue || !cmd.StartDate.HasValue || end.Value.Date > cmd.StartDate.Value.Date)
                    .WithMessage(messageService.GetMessage("End date must be after start date."));

                RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .When(x => x.Quantity.HasValue)
                    .WithMessage(messageService.GetMessage("Quantity must be 0 or greater."));
            });
        }
    }
}

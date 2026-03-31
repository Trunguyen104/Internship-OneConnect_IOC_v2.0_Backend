using System;
using FluentValidation;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using IOCv2.Application.Extensions.Jobs;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public class CreateJobPostingValidator : AbstractValidator<CreateJobPostingCommand>
    {
        private readonly IMessageService _messageService;
        public CreateJobPostingValidator(IMessageService messageService)
        {
            _messageService = messageService;
            // Title and basic text fields
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TitleRequired))
                .MaximumLength(255)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TitleTooLong));

            RuleFor(x => x.Description)
                .MaximumLength(4000)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.DescriptionTooLong));

            RuleFor(x => x.Requirements)
                .MaximumLength(4000)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.RequirementsTooLong));
            RuleFor(x => x.Benefit)
                .MaximumLength(2000)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.BenefitTooLong));

            RuleFor(x => x.Location)
                .NotEmpty()
                .MaximumLength(255)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.LocationTooLong));

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .When(x => x.Quantity.HasValue)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.QuantityMustBePositive));

            // ExpireDate (if provided) must be today or in the future
            RuleFor(x => x.ExpireDate)
                .Must(date => date == null || date.Value.Date >= DateTime.UtcNow.Date)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ExpireDateMustBeTodayOrLater));

            RuleFor(x => x.InternshipPhaseId)
                .NotEmpty()
                .WithMessage("Intern phase is required.");
            // Audience rules
            RuleFor(x => x.Audience)
                .IsInEnum()
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.AudienceInvalid));

            When(x => x.Audience == JobAudience.Targeted, () =>
            {
                RuleFor(x => x.UniversityId)
                    .NotEmpty()
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.UniversityRequiredForTargetAudience));
            });
        }
    }
}

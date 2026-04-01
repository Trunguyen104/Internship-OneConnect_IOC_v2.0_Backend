using FluentValidation;
using IOCv2.Application.Features.Jobs.Commands.CreateJobPosting;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using IOCv2.Application.Extensions.Jobs;
using System;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobDraft
{
    public class CreateJobDraftValidator : AbstractValidator<CreateJobDraftCommand>
    {
        private readonly IMessageService _messageService;

        public CreateJobDraftValidator(IMessageService messageService)
        {
            _messageService = messageService;

            // Title: optional for draft; if provided must be non-empty and within length
            When(x => x.Title != null, () =>
            {
                RuleFor(x => x.Title)
                    .NotEmpty()
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TitleRequired))
                    .MaximumLength(255)
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TitleTooLong));
            });

            // Text fields: optional; validate length only when provided
            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(4000)
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.DescriptionTooLong));
            });

            When(x => x.Requirements != null, () =>
            {
                RuleFor(x => x.Requirements)
                    .MaximumLength(4000)
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.RequirementsTooLong));
            });

            When(x => x.Benefit != null, () =>
            {
                RuleFor(x => x.Benefit)
                    .MaximumLength(2000)
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.BenefitTooLong));
            });

            // Location: optional; if provided must be non-empty and within length
            When(x => x.Location != null, () =>
            {
                RuleFor(x => x.Location)
                    .NotEmpty()
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.LocationRequired))
                    .MaximumLength(255)
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.LocationTooLong));
            });

            // ExpireDate: optional; if provided must be today or later
            RuleFor(x => x.ExpireDate)
                .Must(date => date == null || date.Value.Date >= DateTime.UtcNow.Date)
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ExpireDateMustBeTodayOrLater));
        }
    }
}

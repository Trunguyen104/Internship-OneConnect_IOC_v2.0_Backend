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
        private readonly IMessageService _messageService;
        public UpdateJobValidator(IMessageService messageService)
        {
            this._messageService = messageService;
            RuleFor(x => x.Status.ToString())
                .Must(status => JobsPostingParam.UpdateJobPosting.AllowedStatusForUpdate.Contains(status))
                .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.InvalidStatus));

            When(x => x.Status == JobStatus.PUBLISHED, () =>
            {
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
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.LocationRequired))
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

                // Audience rules
                RuleFor(x => x.Audience)
                    .IsInEnum()
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.AudienceInvalid));

                When(x => x.Audience == JobAudience.Targeted, () =>
                {
                    RuleFor(x => x.UniversityIds)
                        .NotEmpty()
                        .Must(list => list != null && list.Count == 1)
                        .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TargetedRequiresSingleUniversity));
                });
            });

            When(x => x.Status == JobStatus.DRAFT, () =>
            {
                RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .When(x => x.Quantity.HasValue)
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.QuantityMustBePositive));

                RuleFor(x => x.Title)
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

                // Audience rules
                RuleFor(x => x.Audience)
                    .IsInEnum()
                    .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.AudienceInvalid));

                When(x => x.Audience == JobAudience.Targeted, () =>
                {
                    RuleFor(x => x.UniversityIds)
                        .NotEmpty()
                        .Must(list => list != null && list.Count == 1)
                        .WithMessage(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TargetedRequiresSingleUniversity));
                });
            });
        }
    }
}

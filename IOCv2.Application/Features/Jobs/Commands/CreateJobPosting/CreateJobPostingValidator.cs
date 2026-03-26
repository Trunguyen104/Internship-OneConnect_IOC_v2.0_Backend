using System;
using FluentValidation;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public class CreateJobPostingValidator : AbstractValidator<CreateJobPostingCommand>
    {
        public CreateJobPostingValidator(IMessageService messageService, IUnitOfWork unitOfWork)
        {
            // Title and basic text fields
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Title is required."));

            RuleFor(x => x.Description)
                .MaximumLength(4000)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Description is too long."));

            RuleFor(x => x.Requirements)
                .MaximumLength(4000)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Requirements is too long."));

            RuleFor(x => x.Benefit)
                .MaximumLength(2000)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Benefit is too long."));

            RuleFor(x => x.Location)
                .MaximumLength(255)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Location is too long."));

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Quantity.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Quantity must be 0 or greater."));

            // ExpireDate (if provided) must be today or in the future
            RuleFor(x => x.ExpireDate)
                .Must(date => date == null || date.Value.Date >= DateTime.UtcNow.Date)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Expire date must be today or later."));

            // Internship period: required and valid
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Start date is required."))
                .Must(d => d.Date >= DateTime.UtcNow.Date)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Start date must be today or later."));

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "End date is required."))
                .Must((cmd, end) => end.Date > cmd.StartDate.Date)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "End date must be after start date."));

            // Audience rules
            RuleFor(x => x.Audience)
                .IsInEnum()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "Invalid audience."));

            When(x => x.Audience == JobAudience.Targeted, () =>
            {
                RuleFor(x => x.UniversityId)
                    .NotEmpty()
                    .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest, "University must be provided for targeted audience."))
                    .MustAsync(async (id, ct) =>
                    {
                        if (id == null) return false;
                        return await unitOfWork.Repository<University>().ExistsAsync(u => u.UniversityId == id && u.DeletedAt == null, ct);
                    })
                    .WithMessage(messageService.GetMessage(MessageKeys.University.NotFound));
            });
        }
    }
}

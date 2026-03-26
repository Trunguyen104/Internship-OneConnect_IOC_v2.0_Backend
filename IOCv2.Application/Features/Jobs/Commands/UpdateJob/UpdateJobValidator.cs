using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Linq;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    internal class UpdateJobValidator : AbstractValidator<UpdateJobCommand>
    {
        public UpdateJobValidator(IMessageService messageService)
        {
            RuleFor(x => x.JobId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.IdRequired));

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(messageService.GetMessage("Job.TitleRequired"))
                .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.Validation.NameMaxLength));

            RuleFor(x => x)
                .Must(x => x.StartDate <= x.EndDate)
                .WithMessage(messageService.GetMessage("Job.StartBeforeEnd"))
                .When(x => x.StartDate != default && x.EndDate != default);

            RuleFor(x => x.ExpireDate)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage(messageService.GetMessage("Job.ExpireDateMustBeFuture"))
                .When(x => x.ExpireDate.HasValue);

            RuleFor(x => x)
                .Must(x => x.Audience != Domain.Enums.JobAudience.Targeted || (x.UniversityIds != null && x.UniversityIds.Any()))
                .WithMessage(messageService.GetMessage("Job.TargetedRequiresUniversity"));

            // AC-05 note: Targeted should be exactly one university in this domain (business rule)
            RuleFor(x => x)
                .Must(x => x.Audience != Domain.Enums.JobAudience.Targeted || (x.UniversityIds != null && x.UniversityIds.Count == 1))
                .WithMessage(messageService.GetMessage("Job.TargetedRequiresSingleUniversity"));
        }
    }
}

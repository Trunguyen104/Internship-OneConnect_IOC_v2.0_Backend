using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    internal class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
    {
        private readonly IMessageService _messageService;
        public UpdateProjectValidator(IMessageService messageService)
        {
            _messageService = messageService;
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Projects.ProjectIdRequired));

            RuleFor(x => x.ProjectName)
                .MaximumLength(255).WithMessage(_messageService.GetMessage(MessageKeys.Projects.ProjectNameMaxLength));

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.Projects.DescriptionMaxLength));

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage(_messageService.GetMessage(MessageKeys.Projects.StartDateInvalidRange))
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage(_messageService.GetMessage(MessageKeys.Projects.EndDateInvalidRange))
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.Field)
                .MaximumLength(100).WithMessage(_messageService.GetMessage(MessageKeys.Projects.FieldMaxLength))
                .When(x => x.Field != null);

            RuleFor(x => x.Requirements)
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.Projects.RequirementsMaxLength))
                .When(x => x.Requirements != null);

            RuleFor(x => x.Deliverables)
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.Projects.DeliverablesMaxLength))
                .When(x => x.Deliverables != null);

            RuleForEach(x => x.Links).ChildRules(link =>
            {
                link.RuleFor(x => x.ResourceName)
                    .MaximumLength(255).WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.ResourceNameMaxLength));

                link.RuleFor(x => x.Url)
                    .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidExternalUrl))
                    .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidExternalUrl));
            });

            RuleFor(x => x.ResourceDeleteIds)
                .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleForEach(x => x.ResourceDeleteIds)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleFor(x => x.ResourceUpdates)
                .Must(items => items == null || items.Select(i => i.ProjectResourceId).Distinct().Count() == items.Count)
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleForEach(x => x.ResourceUpdates).ChildRules(resource =>
            {
                resource.RuleFor(x => x.ProjectResourceId)
                    .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

                resource.RuleFor(x => x.ResourceName)
                    .MaximumLength(255).WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.ResourceNameMaxLength))
                    .When(x => x.ResourceName != null);

                resource.RuleFor(x => x.ExternalUrl)
                    .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidExternalUrl));
            });

            RuleFor(x => x)
                .Must(x => x.ResourceDeleteIds == null || x.ResourceUpdates == null ||
                           !x.ResourceUpdates.Any(u => x.ResourceDeleteIds.Contains(u.ProjectResourceId)))
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleFor(x => x)
                .Must(x => (x.Files?.Count ?? 0) + (x.Links?.Count ?? 0) + (x.ResourceUpdates?.Count ?? 0) + (x.ResourceDeleteIds?.Count ?? 0) > 0
                           || x.ProjectName != null
                           || x.Description != null
                           || x.StartDate.HasValue
                           || x.EndDate.HasValue
                           || x.Field != null
                           || x.Requirements != null
                           || x.Deliverables != null
                           || x.Template.HasValue
                           || x.InternshipGroupId.HasValue)
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));
        }
    }
}

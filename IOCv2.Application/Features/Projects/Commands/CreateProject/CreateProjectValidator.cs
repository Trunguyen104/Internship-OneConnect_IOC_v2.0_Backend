using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.UserManagement.Commands.CreateUser;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
    {
        private readonly IMessageService _messageService;
        public CreateProjectValidator(IMessageService messageService) {
            _messageService = messageService;
            RuleFor(x => x.InternshipId)
                .NotEqual(Guid.Empty)
                .WithMessage(_messageService.GetMessage(MessageKeys.Projects.ProjectsInternshipIdRequired))
                .When(x => x.InternshipId.HasValue);

            RuleFor(x => x.ProjectName)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Projects.ProjectsProjectNameRequired))
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
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Projects.FieldRequired))
                .MaximumLength(100).WithMessage(_messageService.GetMessage(MessageKeys.Projects.FieldMaxLength));

            RuleFor(x => x.Requirements)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Projects.RequirementsRequired))
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.Projects.RequirementsMaxLength));

            RuleFor(x => x.Deliverables)
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.Projects.DeliverablesMaxLength))
                .When(x => x.Deliverables != null);
        }
    }
}

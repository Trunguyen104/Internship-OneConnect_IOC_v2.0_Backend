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
        }
    }
}

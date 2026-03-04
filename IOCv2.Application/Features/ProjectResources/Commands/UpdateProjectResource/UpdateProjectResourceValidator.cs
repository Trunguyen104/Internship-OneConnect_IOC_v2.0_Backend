using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    internal class UpdateProjectResourceValidator : AbstractValidator<UpdateProjectResourceCommand>
    {
        private readonly IMessageService _messageService;
        public UpdateProjectResourceValidator(IMessageService messageService)
        {
            _messageService = messageService;

            RuleFor(x => x.ProjectResourceId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.ProjectIdRequired));

            RuleFor(x => x.ResourceName)
                .MaximumLength(500).WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.ResourceNameMaxLength))
                .When(x => !string.IsNullOrWhiteSpace(x.ResourceName));
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Projects.ProjectIdRequired));

            // ACV-3: Validate Enum string input before parsing in handler.
            RuleFor(x => x.ResourceType)
                .Must(v => string.IsNullOrWhiteSpace(v) || Enum.TryParse<FileType>(v, ignoreCase: true, out _))
                .WithMessage($"ResourceType must be one of: {string.Join(", ", Enum.GetNames<FileType>())}");
        }
    }
}

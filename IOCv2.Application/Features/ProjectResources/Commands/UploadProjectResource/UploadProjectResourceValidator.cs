using FluentValidation;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource
{
    internal class UploadProjectResourceValidator : AbstractValidator<UploadProjectResourceCommand>
    {
        private readonly IMessageService _messageService;
        public UploadProjectResourceValidator(IMessageService messageService)
        {
            _messageService = messageService;
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Projects.ProjectIdRequired));

            // ACV-3: Validate Enum string input before parsing in handler.
            RuleFor(x => x.ResourceType)
                .NotEmpty().WithMessage("ResourceType is required.")
                .Must(v => Enum.TryParse<FileType>(v, ignoreCase: true, out _))
                .WithMessage($"ResourceType must be one of: {string.Join(", ", Enum.GetNames<FileType>())}");

            RuleFor(x => x.File)
                .NotNull().WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.FileRequired));

            RuleFor(x => x.File.FileName)
                .Must(fileName => FileValidationHelper.IsFileExtensionAllowed(fileName))
                .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidFileType, FileValidationHelper.GetAllowedExtensionsString()));

            RuleFor(x => x.File.Length)
                .Must((command, fileSize) => FileValidationHelper.IsFileSizeValid(command.File.FileName, fileSize))
                .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.FileSizeExceeded));
        }
    }

}

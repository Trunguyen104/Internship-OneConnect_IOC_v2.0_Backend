
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

            RuleFor(x => x)
                .Must(x => x.File != null || !string.IsNullOrWhiteSpace(x.ExternalUrl))
                .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.FileOrLinkRequired));

            RuleFor(x => x)
                .Must(x => !(x.File != null && !string.IsNullOrWhiteSpace(x.ExternalUrl)))
                .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.FileAndLinkMutuallyExclusive));

            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File!.FileName)
                    .Must(fileName => FileValidationHelper.IsFileExtensionAllowed(fileName))
                    .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidFileType, FileValidationHelper.GetAllowedExtensionsString()));

                RuleFor(x => x.File!.Length)
                    .Must((command, fileSize) => command.File != null && FileValidationHelper.IsFileSizeValid(command.File.FileName, fileSize))
                    .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.FileSizeExceeded));
            });

            When(x => !string.IsNullOrWhiteSpace(x.ExternalUrl), () =>
            {
                RuleFor(x => x.ExternalUrl!)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                                 && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidExternalUrl));

                RuleFor(x => x.ResourceType)
                    .Must(type => type == FileType.LINK)
                    .WithMessage(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LinkTypeRequired));
            });
        }
    }

}

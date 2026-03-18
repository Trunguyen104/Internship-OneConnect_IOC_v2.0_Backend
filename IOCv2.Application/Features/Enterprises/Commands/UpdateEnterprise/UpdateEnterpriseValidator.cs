using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    public class UpdateEnterpriseValidator : FluentValidation.AbstractValidator<UpdateEnterpriseCommand>
    {
        private readonly IMessageService _messageService;
        public UpdateEnterpriseValidator(IMessageService messageService) {
            _messageService = messageService;
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(255);
            RuleFor(x => x.Industry)
                .MaximumLength(150);
            RuleFor(x => x.Description)
                .MaximumLength(2000);
            RuleFor(x => x.Address)
                .MaximumLength(500);
            RuleFor(x => x.Website)
                .MaximumLength(255)
                .Must(BeValidUrl)
                .When(x => !string.IsNullOrEmpty(x.Website))
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.WebsiteNotValid));
            RuleFor(x => x.LogoUrl)
                .MaximumLength(255)
                .Must(BeValidUrl)
                .When(x => !string.IsNullOrEmpty(x.LogoUrl))
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.logoUrlNotValid));
            RuleFor(x => x.BackgroundUrl)
                .MaximumLength(255)
                .Must(BeValidUrl)
                .When(x => !string.IsNullOrEmpty(x.BackgroundUrl))
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.BackgroundUrlNotValid));


        }
        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            return url.StartsWith("http://") || url.StartsWith("https://");
        }
    }
}

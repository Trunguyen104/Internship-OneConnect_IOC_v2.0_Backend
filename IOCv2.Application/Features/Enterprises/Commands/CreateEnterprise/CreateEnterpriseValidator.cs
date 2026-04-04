using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseValidator : FluentValidation.AbstractValidator<CreateEnterpriseCommand>
    {
        private readonly IMessageService _messageService;
        private const string taxCodePattern = @"^\d{10}$|^\d{10}-\d{3}$|^\d{10}-\d{2}-\d{3}$";
        public CreateEnterpriseValidator(IMessageService messageService) {
            _messageService = messageService;
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.NameRequired))
                .MaximumLength(255);
            RuleFor(x => x.TaxCode)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.TaxCodeRequired))
                .MaximumLength(50)
                .Matches(taxCodePattern)
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.TaxCodeInvalid));
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
            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.ContactEmailInvalid))
                .When(x => !string.IsNullOrEmpty(x.ContactEmail));


        }
        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            return url.StartsWith("http://") || url.StartsWith("https://");
        }
    }
}

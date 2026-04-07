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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private const string taxCodePattern = @"^\d{10}$|^\d{10}-\d{3}$|^\d{10}-\d{2}-\d{3}$";
        public UpdateEnterpriseValidator(IUnitOfWork unitOfWork, IMessageService messageService) {
            _unitOfWork = unitOfWork;
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
            RuleFor(x => x.ContactEmail)
                .MaximumLength(255)
                .EmailAddress()
                .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.ContactEmailInvalid))
                .MustAsync(BeUniqueContactEmail)
                    .WithMessage(_messageService.GetMessage(MessageKeys.Enterprise.ContactEmailAlreadyExists))
                .When(x => !string.IsNullOrEmpty(x.ContactEmail));
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage(_messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));


        }
        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;
            return url.StartsWith("http://") || url.StartsWith("https://");
        }

        private async Task<bool> BeUniqueContactEmail(UpdateEnterpriseCommand request, string? email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email)) return true;

            var existsInEnterprises = await _unitOfWork.Repository<Domain.Entities.Enterprise>()
                .ExistsAsync(e => e.ContactEmail == email && e.EnterpriseId != request.EnterpriseId, cancellationToken);
            
            if (existsInEnterprises) return false;

            var existsInUniversities = await _unitOfWork.Repository<Domain.Entities.University>()
                .ExistsAsync(u => u.ContactEmail == email, cancellationToken);

            return !existsInUniversities;
        }
    }
}

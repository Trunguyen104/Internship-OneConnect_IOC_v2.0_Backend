using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileValidator(IMessageService messageService)
        {
            RuleFor(v => v.FullName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.FullNameRequired))
                .MaximumLength(150).WithMessage(messageService.GetMessage(MessageKeys.Profile.FullNameMaxLength));

            RuleFor(v => v.PhoneNumber)
                .MaximumLength(15).WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneMaxLength))
                .Matches(@"^\d+$").When(v => !string.IsNullOrEmpty(v.PhoneNumber))
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneInvalid));

            RuleFor(v => v.PortfolioUrl)
                .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.Profile.PortfolioUrlMaxLength))
                .Must(LinkMustBeUrl).When(v => !string.IsNullOrEmpty(v.PortfolioUrl))
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.PortfolioUrlInvalid));

            RuleFor(v => v.Bio)
                .MaximumLength(1000).WithMessage(messageService.GetMessage(MessageKeys.Profile.BioMaxLength));

            RuleFor(v => v.Expertise)
                .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.Profile.ExpertiseMaxLength));

            RuleFor(v => v.Department)
                .MaximumLength(150).WithMessage(messageService.GetMessage(MessageKeys.Profile.DepartmentMaxLength));

            RuleFor(v => v.CvFile)
                .Must(file => file == null || file.Length <= 10 * 1024 * 1024)
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.CvFileMaxSize))
                .Must(file =>
                {
                    if (file == null) return true;
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    return extension == ".pdf" || extension == ".doc" || extension == ".docx";
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.CvFileInvalidFormat));
        }

        private bool LinkMustBeUrl(string? link)
        {
            if (string.IsNullOrWhiteSpace(link)) return true;
            return Uri.TryCreate(link, UriKind.Absolute, out _);
        }
    }
}

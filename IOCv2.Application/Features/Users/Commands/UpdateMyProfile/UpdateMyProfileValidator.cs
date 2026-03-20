using FluentValidation;

namespace IOCv2.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileValidator()
        {
            RuleFor(v => v.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(150).WithMessage("Full Name must not exceed 150 characters.");

            RuleFor(v => v.PhoneNumber)
                .MaximumLength(15).WithMessage("Phone Number must not exceed 15 characters.")
                .Matches(@"^\d+$").When(v => !string.IsNullOrEmpty(v.PhoneNumber))
                .WithMessage("Phone Number must contain only digits.");

            RuleFor(v => v.PortfolioUrl)
                .MaximumLength(255).WithMessage("Portfolio URL must not exceed 255 characters.")
                .Must(LinkMustBeUrl).When(v => !string.IsNullOrEmpty(v.PortfolioUrl))
                .WithMessage("Portfolio URL is invalid.");

            RuleFor(v => v.Bio)
                .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.");

            RuleFor(v => v.Expertise)
                .MaximumLength(500).WithMessage("Expertise must not exceed 500 characters.");

            RuleFor(v => v.Department)
                .MaximumLength(150).WithMessage("Department must not exceed 150 characters.");
        }

        private bool LinkMustBeUrl(string? link)
        {
            if (string.IsNullOrWhiteSpace(link)) return true;
            return Uri.TryCreate(link, UriKind.Absolute, out _);
        }
    }
}

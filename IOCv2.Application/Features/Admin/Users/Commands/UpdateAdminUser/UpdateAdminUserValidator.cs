using FluentValidation;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    internal class UpdateAdminUserValidator : AbstractValidator<UpdateAdminUserCommand>
    {
        public UpdateAdminUserValidator()
        {
            // UserId bắt buộc
            RuleFor(x => x.UserId)
                .NotEmpty();

            // FullName bắt buộc
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(150);

            // PhoneNumber (optional)
            RuleFor(x => x.PhoneNumber)
                .MaximumLength(15)
                .Matches(@"^[0-9+\-\s]*$")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage("Invalid phone number format.");

            // Status (optional nhưng nếu có phải hợp lệ enum)
            RuleFor(x => x.Status)
                .Must(status =>
                    string.IsNullOrWhiteSpace(status) ||
                    Enum.TryParse<UserStatus>(status, true, out _))
                .WithMessage("Invalid status value.");

            // Gender (optional nhưng nếu có phải hợp lệ enum)
            RuleFor(x => x.Gender)
                .Must(gender =>
                    string.IsNullOrWhiteSpace(gender) ||
                    Enum.TryParse<UserGender>(gender, true, out _))
                .WithMessage("Invalid gender value.");

            // DateOfBirth (optional nhưng nếu có phải đúng format)
            RuleFor(x => x.DateOfBirth)
                .Must(dob =>
                    string.IsNullOrWhiteSpace(dob) ||
                    DateOnly.TryParse(dob, out _))
                .WithMessage("Invalid date format. Expected yyyy-MM-dd.");

            // AvatarUrl (optional)
            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
        }
    }
}

using FluentValidation;
using IOCv2.Domain.Enums;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    internal class UpdateAdminUserValidator : AbstractValidator<UpdateAdminUserCommand>
    {
        public UpdateAdminUserValidator(IMessageService messageService)
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
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidPhone));

            // Status (optional nhưng nếu có phải hợp lệ enum)
            RuleFor(x => x.Status)
                .Must(status =>
                    Enum.TryParse<UserStatus>(status, true, out _))
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));

            // Gender (optional nhưng nếu có phải hợp lệ enum)
            RuleFor(x => x.Gender)
                .Must(gender =>
                    Enum.TryParse<UserGender>(gender, true, out _))
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidGender));

            // DateOfBirth (optional nhưng nếu có phải đúng format)
            RuleFor(x => x.DateOfBirth)
                .Must(dob =>
                    DateOnly.TryParse(dob, out _))
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidDateFormat));

            // AvatarUrl (optional)
            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
        }
    }
}

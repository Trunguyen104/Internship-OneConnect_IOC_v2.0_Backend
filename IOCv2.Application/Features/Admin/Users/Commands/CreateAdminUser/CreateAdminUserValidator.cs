using FluentValidation;
using IOCv2.Domain.Enums;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser
{
    internal class CreateAdminUserValidator : AbstractValidator<CreateAdminUserCommand>
    {
        public CreateAdminUserValidator(IMessageService messageService)
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            //RuleFor(x => x.Password)
            //    .NotEmpty()
            //    .MinimumLength(8);

            // Role (required and must be valid enum)
            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidRole));

            // UnitId is required for most roles
            RuleFor(x => x.UnitId)
                .NotEmpty()
                .When(x =>
                {
                    return x.Role == UserRole.SchoolAdmin ||
                           x.Role == UserRole.EnterpriseAdmin ||
                           x.Role == UserRole.HR ||
                           x.Role == UserRole.Mentor ||
                           x.Role == UserRole.Student;
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserUnitRequired));
        }
    }
}

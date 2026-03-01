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
                .NotEmpty()
                .Must(role => Enum.TryParse<UserRole>(role, true, out _))
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidRole));

            // UnitId is required for most roles
            RuleFor(x => x.UnitId)
                .NotEmpty()
                .When(x =>
                {
                    if (Enum.TryParse<UserRole>(x.Role, true, out var role))
                    {
                        return role == UserRole.SchoolAdmin ||
                               role == UserRole.EnterpriseAdmin ||
                               role == UserRole.HR ||
                               role == UserRole.Mentor ||
                               role == UserRole.Student;
                    }
                      return false;
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserUnitRequired));
        }
    }
}

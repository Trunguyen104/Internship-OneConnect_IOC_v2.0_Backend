using FluentValidation;
using IOCv2.Domain.Enums;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.CreateUser
{
    internal class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator(IMessageService messageService)
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

            // TermId is required when creating a Student
            RuleFor(x => x.TermId)
                .NotNull()
                .NotEmpty()
                .When(x => x.Role == UserRole.Student)
                .WithMessage("A student must be linked to an internship term.");
        }
    }
}

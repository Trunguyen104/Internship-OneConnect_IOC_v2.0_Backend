using FluentValidation;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser
{
    internal class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }
}

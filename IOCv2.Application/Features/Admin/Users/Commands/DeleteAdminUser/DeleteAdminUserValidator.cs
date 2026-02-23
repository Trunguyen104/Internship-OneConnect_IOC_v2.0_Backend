using FluentValidation;

namespace IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser
{
    internal class DeleteAdminUserValidator : AbstractValidator<DeleteAdminUserCommand>
    {
        public DeleteAdminUserValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }
}

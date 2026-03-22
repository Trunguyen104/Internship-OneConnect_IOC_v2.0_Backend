using FluentValidation;
using IOCv2.Domain.Enums;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers
{
    internal class GetUsersValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersValidator(IMessageService messageService)
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100);

            RuleFor(x => x.Role)
                .IsInEnum().When(x => x.Role.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidRole));

            RuleFor(x => x.Status)
                .IsInEnum().When(x => x.Status.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));
        }
    }
}

using FluentValidation;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    internal class GetAdminUsersValidator : AbstractValidator<GetAdminUsersQuery>
    {
        public GetAdminUsersValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100);

            RuleFor(x => x.Role)
                .Must(role => string.IsNullOrWhiteSpace(role) || Enum.TryParse<UserRole>(role, true, out _))
                .WithMessage("Invalid role value.");

            RuleFor(x => x.Status)
                .Must(status => string.IsNullOrWhiteSpace(status) || Enum.TryParse<UserStatus>(status, true, out _))
                .WithMessage("Invalid status value.");
        }
    }
}

using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentList;

internal class GetUniAdminStudentListValidator : AbstractValidator<GetUniAdminStudentListQuery>
{
    public GetUniAdminStudentListValidator(IMessageService messageService)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.PageNumberInvalid));

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.PageSizeInvalid))
            .LessThanOrEqualTo(100)
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.PageSizeTooLarge));

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.StatusInvalid))
            .When(x => x.Status.HasValue);

        RuleFor(x => x.LogbookStatus)
            .IsInEnum()
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.LogbookStatusInvalid))
            .When(x => x.LogbookStatus.HasValue);
    }
}

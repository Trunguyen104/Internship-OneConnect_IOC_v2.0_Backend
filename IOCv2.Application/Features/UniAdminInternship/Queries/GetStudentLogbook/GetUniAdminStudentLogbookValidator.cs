using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbook;

internal class GetUniAdminStudentLogbookValidator : AbstractValidator<GetUniAdminStudentLogbookQuery>
{
    public GetUniAdminStudentLogbookValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.StudentIdRequired));

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.PageNumberInvalid));

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.PageSizeInvalid))
            .LessThanOrEqualTo(12)
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.PageSizeTooLargeLogbook));
    }
}


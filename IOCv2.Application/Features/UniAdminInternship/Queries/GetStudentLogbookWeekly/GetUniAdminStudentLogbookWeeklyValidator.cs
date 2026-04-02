using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookWeekly;

internal class GetUniAdminStudentLogbookWeeklyValidator : AbstractValidator<GetUniAdminStudentLogbookWeeklyQuery>
{
    public GetUniAdminStudentLogbookWeeklyValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.StudentIdRequired));
    }
}


using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;

internal class GetUniAdminStudentDetailValidator : AbstractValidator<GetUniAdminStudentDetailQuery>
{
    public GetUniAdminStudentDetailValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.StudentIdRequired));
    }
}


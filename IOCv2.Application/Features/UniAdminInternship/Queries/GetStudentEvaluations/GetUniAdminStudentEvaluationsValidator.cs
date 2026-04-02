using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentEvaluations;

internal class GetUniAdminStudentEvaluationsValidator : AbstractValidator<GetUniAdminStudentEvaluationsQuery>
{
    public GetUniAdminStudentEvaluationsValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.UniAdminInternship.StudentIdRequired));
    }
}


using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public class GetStudentTermDetailValidator : AbstractValidator<GetStudentTermDetailQuery>
{
    public GetStudentTermDetailValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentTermIdRequired));
    }
}

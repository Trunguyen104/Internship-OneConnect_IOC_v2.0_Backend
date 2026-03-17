using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;

public class RestoreStudentValidator : AbstractValidator<RestoreStudentCommand>
{
    public RestoreStudentValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.NotFound));
    }
}

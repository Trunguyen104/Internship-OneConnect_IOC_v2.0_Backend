using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public class BulkWithdrawStudentsValidator : AbstractValidator<BulkWithdrawStudentsCommand>
{
    public BulkWithdrawStudentsValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound));

        RuleFor(x => x.StudentTermIds)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawEmptyList))
            .Must(ids => ids.Count > 0)
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawEmptyList))
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.NotFound));
    }
}

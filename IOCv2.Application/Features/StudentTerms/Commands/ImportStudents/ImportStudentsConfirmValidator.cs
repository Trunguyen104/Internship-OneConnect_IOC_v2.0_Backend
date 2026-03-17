using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudents;

public class ImportStudentsConfirmValidator : AbstractValidator<ImportStudentsConfirmCommand>
{
    public ImportStudentsConfirmValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound));

        RuleFor(x => x.ValidRecords)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty))
            .Must(records => records != null && records.Count > 0)
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty));

        RuleForEach(x => x.ValidRecords)
            .ChildRules(record =>
            {
                record.RuleFor(r => r.StudentCode)
                    .NotEmpty()
                    .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.RowStudentCodeInvalid));

                record.RuleFor(r => r.FullName)
                    .NotEmpty()
                    .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.RowNameInvalid));

                record.RuleFor(r => r.Email)
                    .NotEmpty()
                    .EmailAddress()
                    .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.RowEmailInvalid));
            });
    }
}

using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsConfirm;

public class ImportStudentsConfirmValidator : AbstractValidator<ImportStudentsConfirmCommand>
{
    public ImportStudentsConfirmValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.TermIdRequired));

        RuleFor(x => x.ValidRecords)
            .NotNull().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.ValidRecordsRequired))
            .Must(r => r != null && r.Count > 0).WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.ValidRecordsMinCount));

        RuleForEach(x => x.ValidRecords).ChildRules(record =>
        {
            record.RuleFor(r => r.StudentCode)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeRequired))
                .Matches(@"^[a-zA-Z0-9\-_\.]+$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeInvalid));

            record.RuleFor(r => r.FullName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameRequired))
                .Matches(@"^[\p{L}\s]+$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameInvalid));

            record.RuleFor(r => r.Email)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailRequired))
                .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailInvalid));

            record.RuleFor(r => r.Phone)
                .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.PhoneInvalid))
                .When(r => !string.IsNullOrWhiteSpace(r.Phone));
        });
    }
}

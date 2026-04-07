using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;

public class AddStudentManualValidator : AbstractValidator<AddStudentManualCommand>
{
    public AddStudentManualValidator(IMessageService messageService)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameRequired))
            .Matches(@"^[\p{L}\s]+$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameInvalid));

        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeRequired))
            .Matches(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]+$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeInvalid));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailRequired))
            .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailInvalid));

        RuleFor(x => x.Phone)
            .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.PhoneInvalid))
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Must(dob => !dob.HasValue || IsAtLeast18(dob.Value))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.DateOfBirthMinAge));
    }

    private static bool IsAtLeast18(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today >= dob.AddYears(18);
    }
}

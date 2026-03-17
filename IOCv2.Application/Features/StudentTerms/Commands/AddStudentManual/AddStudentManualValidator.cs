using System.Text.RegularExpressions;
using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;

public class AddStudentManualValidator : AbstractValidator<AddStudentManualCommand>
{
    private static readonly Regex VietnameseNameRegex = new(@"^[\p{L}\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^(\+84|0)[0-9]{9,10}$", RegexOptions.Compiled);
    private static readonly Regex StudentCodeRegex = new(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);

    public AddStudentManualValidator(IMessageService messageService)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameRequired))
            .Must(n => VietnameseNameRegex.IsMatch(n.Trim()))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameInvalid));

        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeRequired))
            .Must(c => StudentCodeRegex.IsMatch(c.Trim()))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeInvalid));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailRequired))
            .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailInvalid));

        RuleFor(x => x.Phone)
            .Must(p => p == null || PhoneRegex.IsMatch(p.Trim()))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.PhoneInvalid));

        RuleFor(x => x.DateOfBirth)
            .Must(BeValidAge)
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.DobInvalid));
    }

    private bool BeValidAge(DateOnly? dob)
    {
        if (!dob.HasValue) return true;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dob.Value.Year;
        if (dob.Value.AddYears(age) > today) age--;
        return age >= 15;
    }
}

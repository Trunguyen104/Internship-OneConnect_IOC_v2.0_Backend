using System.Text.RegularExpressions;
using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public class UpdateStudentTermValidator : AbstractValidator<UpdateStudentTermCommand>
{
    private static readonly Regex VietnameseNameRegex =
        new(@"^[\p{L}\s]+$", RegexOptions.Compiled);

    private static readonly Regex PhoneRegex =
        new(@"^(\+84|0)[0-9]{9,10}$", RegexOptions.Compiled);

    public UpdateStudentTermValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.NotFound));

        RuleFor(x => x.FullName)
            .Must(name => name == null || (name.Trim().Length > 0 && VietnameseNameRegex.IsMatch(name.Trim())))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameInvalid));

        RuleFor(x => x.Email)
            .Must(email => email == null || IsValidEmail(email.Trim()))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailInvalid));

        RuleFor(x => x.Phone)
            .Must(phone => phone == null || PhoneRegex.IsMatch(phone.Trim()))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.PhoneInvalid));

        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob == null || IsValidAge(dob.Value))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.DobInvalid));

        // When Placed is set, EnterpriseId must be provided
        RuleFor(x => x.EnterpriseId)
            .NotEmpty()
            .When(x => x.PlacementStatus == Domain.Enums.PlacementStatus.Placed && x.EnterpriseId == null)
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.PlacedButNoEnterprise));
    }

    private static bool IsValidEmail(string email)
    {
        try { _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }

    private static bool IsValidAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dob.Year;
        if (dob.AddYears(age) > today) age--;
        return age >= 15;
    }
}

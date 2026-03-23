using System.Text.RegularExpressions;
using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public class UpdateStudentTermValidator : AbstractValidator<UpdateStudentTermCommand>
{
    private static readonly Regex StudentCodeRegex = new(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);

    public UpdateStudentTermValidator(IMessageService messageService)
    {
        RuleFor(x => x.StudentCode)
            .Matches(StudentCodeRegex).WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeInvalidDetail))
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode));

        RuleFor(x => x.FullName)
            .Matches(@"^[\p{L}\s]+$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FullNameInvalid))
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EmailInvalid))
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.PhoneInvalid))
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Must(dob => !dob.HasValue || IsAtLeast18(dob.Value))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.DateOfBirthMinAge));

        RuleFor(x => x.EnterpriseId)
            .NotNull().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EnterpriseIdRequiredWhenPlaced))
            .When(x => x.PlacementStatus == PlacementStatus.Placed);
    }

    private static bool IsAtLeast18(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today >= dob.AddYears(18);
    }
}

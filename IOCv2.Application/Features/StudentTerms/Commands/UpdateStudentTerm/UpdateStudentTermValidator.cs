using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public class UpdateStudentTermValidator : AbstractValidator<UpdateStudentTermCommand>
{
    public UpdateStudentTermValidator(IMessageService messageService)
    {
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
            .Must(dob => !dob.HasValue || IsAtLeast15(dob.Value))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.DateOfBirthMinAge));

        RuleFor(x => x.EnterpriseId)
            .NotNull().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.EnterpriseIdRequiredWhenPlaced))
            .When(x => x.PlacementStatus == PlacementStatus.Placed);
    }

    private static bool IsAtLeast15(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today.Year - dob.Year >= 15;
    }
}

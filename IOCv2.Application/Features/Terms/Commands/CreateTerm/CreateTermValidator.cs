using System.Text.RegularExpressions;
using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Terms.Commands.CreateTerm;

public class CreateTermValidator : AbstractValidator<CreateTermCommand>
{
    // Regex to prevent XSS: block <, >, &lt;, &gt;, script tags, and other dangerous characters
    private static readonly Regex XssPattern = new(@"<[^>]*>|&lt;|&gt;|javascript:|on\w+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CreateTermValidator(IMessageService messageService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.NameRequired))
            .MaximumLength(255)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.NameMaxLength))
            .Must(NotContainXssCharacters)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.NameContainsDangerousCharacters));

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.StartDateRequired))
            .Must(BeValidDate)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.InvalidDateFormat))
            .Must(BeAtLeastOneWeekAheadOrMore)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.StartDateMustBeOneWeekAhead));

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.EndDateRequired))
            .Must(BeValidDate)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.InvalidDateFormat))
            .GreaterThan(x => x.StartDate)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.EndDateMustBeAfterStart));
    }

    private bool BeValidDate(DateOnly date)
    {
        return date != default;
    }

    private bool BeAtLeastOneWeekAheadOrMore(DateOnly startDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // If startDate is today, return false (not allowed)
        if (startDate == today)
            return false;
        // If startDate is in the future, it must be at least 7 days ahead
        return startDate >= today.AddDays(7);
    }

    private bool NotContainXssCharacters(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        return !XssPattern.IsMatch(name);
    }
}
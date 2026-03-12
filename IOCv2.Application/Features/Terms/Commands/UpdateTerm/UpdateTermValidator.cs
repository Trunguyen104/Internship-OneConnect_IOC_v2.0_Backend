using System.Text.RegularExpressions;
using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Terms.Commands.UpdateTerm;

public class UpdateTermValidator : AbstractValidator<UpdateTermCommand>
{
    // Regex to prevent XSS: block <, >, &lt;, &gt;, script tags, and other dangerous characters
    private static readonly Regex XssPattern = new(@"<[^>]*>|&lt;|&gt;|javascript:|on\w+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public UpdateTermValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.NameRequired))
            .MaximumLength(255)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.NameMaxLength))
            .Must(NotContainXssCharacters)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.NameContainsDangerousCharacters));

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.StartDateRequired));

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.EndDateRequired))
            .GreaterThan(x => x.StartDate)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.EndDateMustBeAfterStart));

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest));
    }

    private bool NotContainXssCharacters(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        return !XssPattern.IsMatch(name);
    }
}
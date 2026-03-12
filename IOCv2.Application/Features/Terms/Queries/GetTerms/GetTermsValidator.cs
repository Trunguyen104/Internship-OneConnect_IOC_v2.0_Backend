using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Terms.Queries.GetTerms;

public class GetTermsValidator : AbstractValidator<GetTermsQuery>
{
    public GetTermsValidator(IMessageService messageService)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberInvalid));

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeInvalid))
            .LessThanOrEqualTo(100)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeTooLarge));

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm))
            .WithMessage(messageService.GetMessage(MessageKeys.Page.SearchTermMaxLength));

        RuleFor(x => x.Year)
            .GreaterThan(2000)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.YearInvalidRange))
            .LessThanOrEqualTo(2100)
            .WithMessage(messageService.GetMessage(MessageKeys.Terms.YearInvalidRange))
            .When(x => x.Year.HasValue);
    }
}
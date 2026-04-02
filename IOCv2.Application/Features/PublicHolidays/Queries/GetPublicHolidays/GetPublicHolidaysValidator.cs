using FluentValidation;

namespace IOCv2.Application.Features.PublicHolidays.Queries.GetPublicHolidays;

internal sealed class GetPublicHolidaysValidator : AbstractValidator<GetPublicHolidaysQuery>
{
    public GetPublicHolidaysValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");
    }
}

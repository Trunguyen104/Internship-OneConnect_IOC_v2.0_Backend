using FluentValidation;

namespace IOCv2.Application.Features.PublicHolidays.Commands.SyncPublicHolidays;

internal sealed class SyncPublicHolidaysValidator : AbstractValidator<SyncPublicHolidaysCommand>
{
    public SyncPublicHolidaysValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2)
            .WithMessage("CountryCode must be a 2-letter ISO 3166-1 alpha-2 code (e.g. VN).");
    }
}

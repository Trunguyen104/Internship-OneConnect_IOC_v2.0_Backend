using FluentValidation;

namespace IOCv2.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;

internal sealed class CreatePublicHolidayValidator : AbstractValidator<CreatePublicHolidayCommand>
{
    public CreatePublicHolidayValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required.");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .WithMessage("Description must not exceed 200 characters.")
            .When(x => x.Description is not null);
    }
}

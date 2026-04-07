using FluentValidation;

namespace IOCv2.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;

internal sealed class DeletePublicHolidayValidator : AbstractValidator<DeletePublicHolidayCommand>
{
    public DeletePublicHolidayValidator()
    {
        RuleFor(x => x.PublicHolidayId)
            .NotEmpty()
            .WithMessage("PublicHolidayId is required.");
    }
}

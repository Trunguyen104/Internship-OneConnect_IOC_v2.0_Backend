using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicValidator : AbstractValidator<DeleteEpicCommand>
{
    public DeleteEpicValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.EpicId)
            .NotEmpty()
            .WithMessage(localizer["Epic.ProjectIdRequired"]);
    }
}

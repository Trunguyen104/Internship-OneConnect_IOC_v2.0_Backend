using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public class CreateEpicValidator : AbstractValidator<CreateEpicCommand>
{
    public CreateEpicValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage(localizer["Epic.ProjectIdRequired"]);
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localizer["Epic.NameRequired"])
            .MaximumLength(255)
            .WithMessage(localizer["Epic.NameMaxLength"]);
        
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage(localizer["Epic.DescriptionMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

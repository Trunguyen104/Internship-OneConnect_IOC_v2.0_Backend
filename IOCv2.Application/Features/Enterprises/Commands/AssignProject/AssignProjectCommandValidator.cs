using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignProject;

public class AssignProjectCommandValidator : AbstractValidator<AssignProjectCommand>
{
    public AssignProjectCommandValidator(IMessageService messageService)
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ApplicationIdRequired));

        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ProjectNameRequired))
            .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ProjectNameMaxLength));

        RuleFor(x => x.ProjectDescription)
            .MaximumLength(1000).WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ProjectDescriptionMaxLength));
    }
}

using FluentValidation;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById;

internal class GetProjectByIdValidator : AbstractValidator<GetProjectByIdQuery>
{
    public GetProjectByIdValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Dự án ID không được trống.");
    }
}

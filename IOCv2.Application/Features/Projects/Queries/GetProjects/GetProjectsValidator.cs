using FluentValidation;

namespace IOCv2.Application.Features.Projects.Queries.GetProjects;

internal class GetProjectsValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100);
    }
}

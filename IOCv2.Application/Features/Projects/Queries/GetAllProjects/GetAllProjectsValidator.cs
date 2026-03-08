using FluentValidation;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetAllProjects
{
    public class GetAllProjectsValidator : AbstractValidator<GetAllProjectsQuery>
    {
        public GetAllProjectsValidator()
        {
            RuleFor(x => x.Status)
                .Must(v => string.IsNullOrEmpty(v) || Enum.TryParse<ProjectStatus>(v, true, out _))
                .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<ProjectStatus>())}");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");
        }
    }
}

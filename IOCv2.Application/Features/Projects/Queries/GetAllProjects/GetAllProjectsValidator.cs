using FluentValidation;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetAllProjects
{
    public class GetAllProjectsValidator : AbstractValidator<GetAllProjectsQuery>
    {
        public GetAllProjectsValidator()
        {
            RuleFor(x => x.VisibilityStatus)
                .IsInEnum()
                .When(x => x.VisibilityStatus.HasValue);

            RuleFor(x => x.OperationalStatus)
                .IsInEnum()
                .When(x => x.OperationalStatus.HasValue);


            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");
        }
    }
}

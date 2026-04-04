using FluentValidation;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItems;

internal class GetWorkItemsValidator : AbstractValidator<GetWorkItemsQuery>
{
    public GetWorkItemsValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100);
    }
}

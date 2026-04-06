using FluentValidation;
using System;

namespace IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm
{
    public class GetStudentsByTermValidator : AbstractValidator<GetStudentsByTermQuery>
    {
        private const int MaxPageSize = 100;

        public GetStudentsByTermValidator()
        {
            RuleFor(x => x.TermId)
                .NotEmpty()
                .WithMessage("TermId is required.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.")
                .When(x => x.PageNumber.HasValue);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageSize must be at least 1.")
                .LessThanOrEqualTo(MaxPageSize)
                .WithMessage($"PageSize must be at most {MaxPageSize}.")
                .When(x => x.PageSize.HasValue);
        }
    }
}

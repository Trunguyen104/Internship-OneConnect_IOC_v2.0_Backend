using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    internal class GetPlacedStudentsQueryValidator : AbstractValidator<GetPlacedStudentsQuery>
    {
        public GetPlacedStudentsQueryValidator()
        {
            // PhaseId là optional — nếu không truyền, backend tự tìm phase Active/Upcoming

            RuleFor(v => v.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage(MessageKeys.Page.PageNumberMinValue);

            RuleFor(v => v.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage(MessageKeys.Page.PageSizeMinValue)
                .LessThanOrEqualTo(100).WithMessage(MessageKeys.Page.PageSizeMaxValue);

            RuleFor(v => v.SearchTerm)
                .MaximumLength(100).WithMessage(MessageKeys.Page.SearchTermMaxLength);
        }
    }
}

using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudents;

public class GetStudentsValidator : AbstractValidator<GetStudentsQuery>
{
    private static readonly string[] AllowedSortBy =
        { "fullname", "studentcode", "placementstatus", "enrollmentdate" };

    private static readonly string[] AllowedSortOrder = { "asc", "desc" };

    public GetStudentsValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage("TermId không được để trống");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberInvalid));

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeInvalid));

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Từ khóa tìm kiếm không vượt quá 100 ký tự")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortBy.Contains(s.ToLower()))
            .WithMessage($"SortBy phải là một trong: {string.Join(", ", AllowedSortBy)}")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(s => AllowedSortOrder.Contains(s.ToLower()))
            .WithMessage("SortOrder phải là 'asc' hoặc 'desc'")
            .When(x => !string.IsNullOrWhiteSpace(x.SortOrder));
    }
}

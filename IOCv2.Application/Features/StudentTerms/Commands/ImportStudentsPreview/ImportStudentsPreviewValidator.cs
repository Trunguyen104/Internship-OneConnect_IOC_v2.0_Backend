using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsPreview;

public class ImportStudentsPreviewValidator : AbstractValidator<ImportStudentsPreviewCommand>
{
    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public ImportStudentsPreviewValidator()
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage("TermId không được để trống");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File không được để trống")
            .Must(f => f != null && f.Length > 0).WithMessage("File không được rỗng")
            .Must(f => f == null || f.Length <= MaxFileSizeBytes)
                .WithMessage("File không được vượt quá 5MB")
            .Must(f => f == null || AllowedExtensions.Contains(
                System.IO.Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("Chỉ chấp nhận file .xlsx hoặc .xls");
    }
}

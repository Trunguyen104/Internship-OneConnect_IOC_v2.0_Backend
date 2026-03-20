using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsPreview;

public class ImportStudentsPreviewValidator : AbstractValidator<ImportStudentsPreviewCommand>
{
    private const string AllowedExtension = ".xlsx";
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public ImportStudentsPreviewValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.TermIdRequired));

        RuleFor(x => x.File)
            .NotNull().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileRequired))
            .Must(f => f != null && f.Length > 0)
                .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty))
            .Must(f => f == null || f.Length <= MaxFileSizeBytes)
                .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileTooLarge))
            .Must(f => f == null || string.Equals(
                System.IO.Path.GetExtension(f.FileName),
                AllowedExtension,
                System.StringComparison.OrdinalIgnoreCase))
                .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.InvalidFileFormat));
    }
}

using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudents;

public class ImportStudentsPreviewValidator : AbstractValidator<ImportStudentsPreviewCommand>
{
    private static readonly string[] AllowedExtensions = { ".xlsx" };
    private const int MaxFileSizeMb = 5;

    public ImportStudentsPreviewValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound));

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty))
            .Must(f => f != null && f.Length > 0)
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty))
            .Must(f => f != null && f.Length <= MaxFileSizeMb * 1024 * 1024)
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileSizeExceeded))
            .Must(f => f != null && AllowedExtensions.Contains(
                System.IO.Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.FileInvalidFormat));
    }
}

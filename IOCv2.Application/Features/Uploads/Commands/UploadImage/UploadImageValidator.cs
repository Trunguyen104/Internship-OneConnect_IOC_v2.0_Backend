using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace IOCv2.Application.Features.Uploads.Commands.UploadImage;

public class UploadImageValidator : AbstractValidator<UploadImageCommand>
{
    private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".svg", ".gif" };

    public UploadImageValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("File is required")
            .Must(ValidateSize).WithMessage("File size exceeds limit (10MB)")
            .Must(ValidateExtension).WithMessage("File type is not allowed");

        RuleFor(v => v.Folder)
            .NotEmpty().WithMessage("Folder is required");
    }

    private bool ValidateSize(IFormFile file)
    {
        if (file == null) return false;
        return file.Length <= 10 * 1024 * 1024; // 10MB
    }

    private bool ValidateExtension(IFormFile file)
    {
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return !string.IsNullOrEmpty(ext) && _permittedExtensions.Contains(ext);
    }
}

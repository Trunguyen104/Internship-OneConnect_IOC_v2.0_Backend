using IOCv2.Application.Constants;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Common.Helpers
{
    public static class FileValidationHelper
    {
        /// <summary>
        /// A collection of file extensions that are allowed for upload.
        /// HashSet is used for fast lookup (O(1)) when validating file extensions.
        /// StringComparer.OrdinalIgnoreCase ensures the comparison is case-insensitive
        /// (e.g., ".PDF", ".Pdf", ".pdf" are treated the same).
        /// </summary>
        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            FileConstants.PdfExtension,
            FileConstants.DocxExtension,
            FileConstants.PptxExtension,
            FileConstants.ZipExtension,
            FileConstants.RarExtension,
            FileConstants.JpgExtension,
            FileConstants.JpegExtension,
            FileConstants.PngExtension
        };

        /// <summary>
        /// Maps file extensions to their corresponding <see cref="FileType"/>.
        /// This dictionary is used to determine the file type based on the file extension.
        /// StringComparer.OrdinalIgnoreCase ensures extension matching is case-insensitive
        /// (e.g., ".PDF" and ".pdf" map to the same FileType).
        /// </summary>
        private static readonly Dictionary<string, FileType> _extensionToTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            [FileConstants.PdfExtension] = FileType.PDF,
            [FileConstants.DocxExtension] = FileType.DOCX,
            [FileConstants.PptxExtension] = FileType.PPTX,
            [FileConstants.ZipExtension] = FileType.ZIP,
            [FileConstants.RarExtension] = FileType.RAR,
            [FileConstants.JpgExtension] = FileType.JPG,
            [FileConstants.JpegExtension] = FileType.JPG,
            [FileConstants.PngExtension] = FileType.PNG
        };

        /// <summary>
        /// Defines the maximum allowed file size (in bytes) for each <see cref="FileType"/>.
        /// Used during file validation to ensure uploaded files do not exceed the permitted size.
        /// </summary>
        private static readonly Dictionary<FileType, long> _maxFileSizes = new()
        {
            [FileType.PDF] = 50 * 1024 * 1024,      // 50MB
            [FileType.DOCX] = 20 * 1024 * 1024,     // 20MB
            [FileType.PPTX] = 50 * 1024 * 1024,     // 50MB
            [FileType.ZIP] = 200 * 1024 * 1024,     // 200MB
            [FileType.RAR] = 200 * 1024 * 1024,     // 200MB
            [FileType.JPG] = 10 * 1024 * 1024,      // 10MB
            [FileType.PNG] = 10 * 1024 * 1024       // 10MB
        };

        /// <summary>
        /// Checks whether the file extension of the provided file name is allowed.
        /// </summary>
        /// <param name="fileName">The name of the file to validate.</param>
        /// <returns>
        /// True if the file extension exists and is included in the allowed extensions list; otherwise false.
        /// </returns>
        public static bool IsFileExtensionAllowed(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Gets the corresponding <see cref="FileType"/> based on the file extension.
        /// </summary>
        /// <param name="fileName">The name of the file used to determine its type.</param>
        /// <returns>
        /// The matched <see cref="FileType"/> if the extension exists in the mapping; otherwise null.
        /// </returns>
        public static FileType? GetFileType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _extensionToTypeMap.TryGetValue(extension, out var fileType) ? fileType : null;
        }

        /// <summary>
        /// Validates whether the file size is within the allowed limit for its file type.
        /// </summary>
        /// <param name="fileName">The name of the file used to determine its type.</param>
        /// <param name="fileSizeInBytes">The size of the file in bytes.</param>
        /// <returns>
        /// True if the file type is recognized and the size does not exceed the configured limit; otherwise false.
        /// </returns>
        public static bool IsFileSizeValid(string fileName, long fileSizeInBytes)
        {
            var fileType = GetFileType(fileName);
            if (!fileType.HasValue)
                return false;

            return _maxFileSizes.TryGetValue(fileType.Value, out var maxSize)
                ? fileSizeInBytes <= maxSize
                : false;
        }

        /// <summary>
        /// Gets the maximum allowed file size (in bytes) for the specified <see cref="FileType"/>.
        /// </summary>
        /// <param name="fileType">The file type used to retrieve the configured maximum size.</param>
        /// <returns>
        /// The maximum allowed file size in bytes if the file type exists in the configuration; otherwise 0.
        /// </returns>
        public static long GetMaxFileSize(FileType fileType)
        {
            return _maxFileSizes.TryGetValue(fileType, out var maxSize) ? maxSize : 0;
        }

        /// <summary>
        /// Returns a comma-separated string of all allowed file extensions.
        /// </summary>
        /// <returns>
        /// A formatted string containing the allowed file extensions.
        /// </returns>
        public static string GetAllowedExtensionsString()
        {
            return string.Join(", ", _allowedExtensions.OrderBy(x => x));
        }

        /// <summary>
        /// Validates a file by checking both its extension and file size.
        /// </summary>
        /// <param name="fileName">The name of the file to validate.</param>
        /// <param name="fileSizeInBytes">The size of the file in bytes.</param>
        /// <returns>
        /// A <see cref="FileValidationResult"/> containing validation status,
        /// error message (if any), and detected file type.
        /// </returns>
        public static FileValidationResult ValidateFile(string fileName, long fileSizeInBytes)
        {
            var result = new FileValidationResult();

            // Check if file extension is allowed
            if (!IsFileExtensionAllowed(fileName))
            {
                result.IsValid = false;
                result.ErrorMessage = string.Format(FileConstants.ErrorFileTypeNotAllowed, GetAllowedExtensionsString());
                return result;
            }

            // Determine file type from extension
            var fileType = GetFileType(fileName);
            result.FileType = fileType;

            // Validate file size based on file type
            if (!IsFileSizeValid(fileName, fileSizeInBytes))
            {
                var max = GetMaxFileSize(fileType!.Value) / (1024 * 1024);
                result.IsValid = false;
                result.ErrorMessage = string.Format(
                        FileConstants.ErrorFileSizeExceeded,
                        max);
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Gets the MIME type corresponding to the provided file name.
        /// </summary>
        /// <param name="fileName">The file name used to determine its MIME type.</param>
        /// <returns>
        /// The MIME type associated with the file type, or the default MIME type if not recognized.
        /// </returns>
        public static string GetMimeType(string fileName)
        {
            var fileType = GetFileType(fileName);
            return fileType?.GetMimeType() ?? FileConstants.DefaultMime;
        }
    }

    /// <summary>
    /// Represents the result of validating a file.
    /// </summary>
    public class FileValidationResult
    {
        /// <summary>
        /// Indicates whether the file passed all validation checks.
        /// </summary>
        public bool IsValid { get; set; }
        /// <summary>
        /// Contains the validation error message if validation fails.
        /// </summary>
        public string? ErrorMessage { get; set; }
        /// <summary>
        /// The detected file type based on the file extension.
        /// </summary>
        public FileType? FileType { get; set; }
    }

    /// <summary>
    /// Provides extension methods related to <see cref="FileType"/>.
    /// </summary>
    public static class FileTypeExtensions
    {
        /// <summary>
        /// Gets the MIME type associated with the specified <see cref="FileType"/>.
        /// </summary>
        /// <param name="fileType">The file type.</param>
        /// <returns>The corresponding MIME type.</returns>
        public static string GetMimeType(this FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF => FileConstants.MimePdf,
                FileType.DOCX => FileConstants.MimeDocx,
                FileType.PPTX => FileConstants.MimePptx,
                FileType.ZIP => FileConstants.MimeZip,
                FileType.RAR => FileConstants.MimeRar,
                FileType.JPG => FileConstants.MimeJpg,
                FileType.PNG => FileConstants.MimePng,
                _ => FileConstants.DefaultMime
            };
        }

        /// <summary>
        /// Gets the default file extension associated with the specified <see cref="FileType"/>.
        /// </summary>
        /// <param name="fileType">The file type.</param>
        /// <returns>The file extension string.</returns>
        public static string GetExtension(this FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF => FileConstants.PdfExtension,
                FileType.DOCX => FileConstants.DocxExtension,
                FileType.PPTX => FileConstants.PptxExtension,
                FileType.ZIP => FileConstants.ZipExtension,
                FileType.RAR => FileConstants.RarExtension,
                FileType.JPG => FileConstants.JpgExtension,
                FileType.PNG => FileConstants.PngExtension,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets a human-readable display name for the specified <see cref="FileType"/>.
        /// </summary>
        /// <param name="fileType">The file type.</param>
        /// <returns>A descriptive display name for the file type.</returns>
        public static string GetDisplayName(this FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF => FileConstants.DisplayNamePdf,
                FileType.DOCX => FileConstants.DisplayNameDocx,
                FileType.PPTX => FileConstants.DisplayNamePptx,
                FileType.ZIP => FileConstants.DisplayNameZip,
                FileType.RAR => FileConstants.DisplayNameRar,
                FileType.JPG => FileConstants.DisplayNameJpg,
                FileType.PNG => FileConstants.DisplayNamePng,
                _ => fileType.ToString()
            };
        }
    }

}

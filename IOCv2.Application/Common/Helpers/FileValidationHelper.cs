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
        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".xlsx", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png"
    };

        private static readonly Dictionary<string, FileType> _extensionToTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = FileType.PDF,
            [".docx"] = FileType.DOCX,
            [".xlsx"] = FileType.XLSX,
            [".pptx"] = FileType.PPTX,
            [".zip"] = FileType.ZIP,
            [".rar"] = FileType.RAR,
            [".jpg"] = FileType.JPG,
            [".jpeg"] = FileType.JPG,
            [".png"] = FileType.PNG
        };

        private static readonly Dictionary<FileType, long> _maxFileSizes = new()
        {
            [FileType.PDF] = 50 * 1024 * 1024,      // 50MB
            [FileType.DOCX] = 20 * 1024 * 1024,     // 20MB
            [FileType.XLSX] = 20 * 1024 * 1024,     // 20MB
            [FileType.PPTX] = 50 * 1024 * 1024,     // 50MB
            [FileType.ZIP] = 200 * 1024 * 1024,     // 200MB
            [FileType.RAR] = 200 * 1024 * 1024,     // 200MB
            [FileType.JPG] = 10 * 1024 * 1024,      // 10MB
            [FileType.PNG] = 10 * 1024 * 1024       // 10MB
        };

        /// <summary>
        /// Kiểm tra file extension có được phép không
        /// </summary>
        public static bool IsFileExtensionAllowed(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Lấy FileType từ file name
        /// </summary>
        public static FileType? GetFileType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _extensionToTypeMap.TryGetValue(extension, out var fileType) ? fileType : null;
        }

        /// <summary>
        /// Kiểm tra kích thước file có vượt quá giới hạn không
        /// </summary>
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
        /// Lấy giới hạn kích thước cho loại file
        /// </summary>
        public static long GetMaxFileSize(FileType fileType)
        {
            return _maxFileSizes.TryGetValue(fileType, out var maxSize) ? maxSize : 0;
        }

        /// <summary>
        /// Lấy danh sách các extension được phép (dạng string)
        /// </summary>
        public static string GetAllowedExtensionsString()
        {
            return string.Join(", ", _allowedExtensions.OrderBy(x => x));
        }

        /// <summary>
        /// Validate file và trả về kết quả chi tiết
        /// </summary>
        public static FileValidationResult ValidateFile(string fileName, long fileSizeInBytes)
        {
            var result = new FileValidationResult();

            // Kiểm tra extension
            if (!IsFileExtensionAllowed(fileName))
            {
                result.IsValid = false;
                result.ErrorMessage = $"File type not allowed. Allowed types: {GetAllowedExtensionsString()}";
                return result;
            }

            // Lấy loại file
            var fileType = GetFileType(fileName);
            result.FileType = fileType;

            // Kiểm tra kích thước
            if (!IsFileSizeValid(fileName, fileSizeInBytes))
            {
                result.IsValid = false;
                result.ErrorMessage = $"File size exceeds maximum allowed ({GetMaxFileSize(fileType!.Value) / (1024 * 1024)}MB)";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Lấy MIME type cho file
        /// </summary>
        public static string GetMimeType(string fileName)
        {
            var fileType = GetFileType(fileName);
            return fileType?.GetMimeType() ?? "application/octet-stream";
        }
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public FileType? FileType { get; set; }
    }

    public static class FileTypeExtensions
    {
        public static string GetMimeType(this FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF => "application/pdf",
                FileType.DOCX => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileType.XLSX => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileType.PPTX => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                FileType.ZIP => "application/zip",
                FileType.RAR => "application/x-rar-compressed",
                FileType.JPG => "image/jpeg",
                FileType.PNG => "image/png",
                FileType.LINK => "text/html",
                _ => "application/octet-stream"
            };
        }

        public static string GetExtension(this FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF => ".pdf",
                FileType.DOCX => ".docx",
                FileType.XLSX => ".xlsx",
                FileType.PPTX => ".pptx",
                FileType.ZIP => ".zip",
                FileType.RAR => ".rar",
                FileType.JPG => ".jpg",
                FileType.PNG => ".png",
                FileType.LINK => string.Empty,
                _ => string.Empty
            };
        }

        public static string GetDisplayName(this FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF => "PDF Document",
                FileType.DOCX => "Word Document",
                FileType.PPTX => "PowerPoint Presentation",
                FileType.ZIP => "ZIP Archive",
                FileType.RAR => "RAR Archive",
                FileType.JPG => "JPEG Image",
                FileType.PNG => "PNG Image",
                FileType.LINK => "External Link",
                _ => fileType.ToString()
            };
        }
    }

}

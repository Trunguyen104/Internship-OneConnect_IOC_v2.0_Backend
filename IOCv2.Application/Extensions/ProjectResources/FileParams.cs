using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Constants
{
    public static class FileParams
    {
        public const string PdfExtension = ".pdf";
        public const string DocxExtension = ".docx";
        public const string PptxExtension = ".pptx";
        public const string ZipExtension = ".zip";
        public const string RarExtension = ".rar";
        public const string JpgExtension = ".jpg";
        public const string JpegExtension = ".jpeg";
        public const string PngExtension = ".png";

        public const string MimePdf = "application/pdf";
        public const string MimeDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public const string MimePptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        public const string MimeZip = "application/zip";
        public const string MimeRar = "application/x-rar-compressed";
        public const string MimeJpg = "image/jpeg";
        public const string MimePng = "image/png";

        public const string DefaultMime = "application/octet-stream";

        public const string ErrorFileTypeNotAllowed = "File type not allowed. Allowed types: {0}";
        public const string ErrorFileSizeExceeded = "File size exceeds maximum allowed ({0}MB)";

        public const string DisplayNamePdf = "PDF Document";
        public const string DisplayNameDocx = "Word Document";
        public const string DisplayNamePptx = "PowerPoint Presentation";
        public const string DisplayNameZip = "ZIP Archive";
        public const string DisplayNameRar = "RAR Archive";
        public const string DisplayNameJpg = "JPEG Image";
        public const string DisplayNamePng = "PNG Image";

        public const string FileDomain = "localhost:5050";
        public static string GetFolder(Guid projectId) {
            return string.Format($"projects/{projectId}/resources");
        }
        public static string GetFileName(string fileName) {
            return $"{Guid.NewGuid():N}_{fileName}";
        }
    }
}
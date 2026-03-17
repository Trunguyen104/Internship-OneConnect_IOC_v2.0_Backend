using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Extensions.ProjectResources
{
    public class ProjectResourceParams
    {
        public class Filter
        {
            public const string Desc = "desc";
            public const string ResourceName = "resourcename";
            public const string ResourceType = "resourcetype";
            public const string CreateDate = "createdat";
        }

        public class FileParams
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

            //public const string ErrorFileTypeNotAllowed = "File type not allowed. Allowed types: {0}";
            //public const string ErrorFileSizeExceeded = "File size exceeds maximum allowed ({0}MB)";

            public const string DisplayNamePdf = "PDF Document";
            public const string DisplayNameDocx = "Word Document";
            public const string DisplayNamePptx = "PowerPoint Presentation";
            public const string DisplayNameZip = "ZIP Archive";
            public const string DisplayNameRar = "RAR Archive";
            public const string DisplayNameJpg = "JPEG Image";
            public const string DisplayNamePng = "PNG Image";

            public const string FileDomain = "localhost:5050";

            //configuration
            public const string ConfigurationStoragePathKey = "FileStorage:Path";
            public const string ConfigurationBaseUrl = "FileStorage:BaseUrl";

            public const string BaseUrl = "/Uploads";

            public const string StoragePath = "{0}/Uploads";

            

            public const string GetFolder = "projects/{0}/resources";
            
            public const string GetFileName = "{0}_{1}";

            public const string GetFileDownloadName = "{0}{1}";

        }
    }
}
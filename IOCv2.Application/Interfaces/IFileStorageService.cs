using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder, string? fileName = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default);
        string GetFileUrl(string filePath);
        Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default);
        Task<FileInfo?> GetFileInfoAsync(string fileUrl, CancellationToken cancellationToken = default);
        string GetFilePathFromUrl(string resourceUrl);
    }
}
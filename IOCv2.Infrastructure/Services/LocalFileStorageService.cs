using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _storagePath;
        private readonly string _baseUrl;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IConfiguration configuration,
            ILogger<LocalFileStorageService> logger)
        {
            _storagePath = configuration["FileStorage:Path"]
                ?? Path.Combine(Directory.GetCurrentDirectory()+ "/Internship-OneConnect_IOC_v2.0_Backend/IOCv2.API/wwwroot", "uploads");

            _baseUrl = configuration["FileStorage:BaseUrl"]
                ?? "/uploads";
            _logger = logger;

            // Create root directory if not exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<string> UploadFileAsync(
            IFormFile file,
            string folder,
            string? fileName = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Create folder path
                var folderPath = Path.Combine(_storagePath, folder);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Generate unique file name
                var uniqueFileName = string.IsNullOrEmpty(fileName)
                    ? $"{Guid.NewGuid():N}_{file.FileName}"
                    : fileName;

                var filePath = Path.Combine(folderPath, uniqueFileName);

                // Save file
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream, cancellationToken);

                // Return relative URL
                var relativePath = Path.Combine(folder, uniqueFileName).Replace('\\', '/');
                var fileUrl = $"{_baseUrl}/{relativePath}";

                _logger.LogInformation("File uploaded successfully: {FilePath}", filePath);

                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath), cancellationToken);
                    _logger.LogInformation("File deleted: {FilePath}", filePath);
                    return true;
                }

                _logger.LogWarning("File not found for deletion: {FileUrl}", fileUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileUrl}", fileUrl);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);
                return await Task.Run(() => File.Exists(filePath), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FileUrl}", fileUrl);
                return false;
            }
        }

        public string GetFileUrl(string filePath)
        {
            var relativePath = filePath.Replace('\\', '/');
            return $"{_baseUrl}/{relativePath}";
        }

        public async Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {fileUrl}");
                }

                var fileStream = File.OpenRead(filePath);

                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream: {FileUrl}", fileUrl);
                throw;
            }
        }

        public async Task<FileInfo?> GetFileInfoAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);

                if (!File.Exists(filePath))
                {
                    return null;
                }

                var fileInfo = new FileInfo(filePath);
                return await Task.Run(() => fileInfo, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info: {FileUrl}", fileUrl);
                return null;
            }
        }

        public string GetFilePathFromUrl(string fileUrl)
        {
            var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.ToString();
            // Decode URL (%20, %E1%BB%8B ...)
            path = Uri.UnescapeDataString(path);
            // remove /uploads/ ở đầu
            var relativePath = path.Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase)
                                   .TrimStart('/');
            return Path.Combine(_storagePath, relativePath);
        }
    }

}

using IOCv2.Application.Extensions.ProjectResources;
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
        private const string _domain = FileParams.FileDomain;
        private readonly IMessageService _messageService;

        public LocalFileStorageService(
            IConfiguration configuration,
            ILogger<LocalFileStorageService> logger, IMessageService messageService)
        {
            _storagePath = configuration["FileStorage:Path"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            _baseUrl = configuration["FileStorage:BaseUrl"]
                ?? "/Uploads";
            _logger = logger;
            _messageService = messageService;

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

                _logger.LogInformation(_messageService.GetMessage("ProjectResources.LogUploadAutoSetFileTypeError"), filePath);

                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage("MessageKeys.File.FileUploadedError"), file.FileName);
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
                    _logger.LogInformation(_messageService.GetMessage("MessageKeys.File.FileDeletedSuccess"), filePath);
                    return true;
                }
                _logger.LogWarning(_messageService.GetMessage("MessageKeys.File.FileNotFound"), fileUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage("MessageKeys.File.FileDeletedError"), fileUrl);
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
                _logger.LogError(ex, _messageService.GetMessage("MessageKeys.File.FileCheckExistsError"), fileUrl);
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
                    throw new FileNotFoundException(_messageService.GetMessage("MessageKeys.File.FileNotFound", fileUrl), fileUrl);
                }

                return await Task.Run(() => File.OpenRead(filePath), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage("MessageKeys.File.FileGetStreamError"), fileUrl);
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
            var relativePath = fileUrl.Replace(_baseUrl, "").Replace("/Uploads", "").TrimStart('/');

            return Path.Combine(_storagePath, relativePath);
        }

        public string GetDomainUrl()
        {
            return _domain;
        }
    }

}
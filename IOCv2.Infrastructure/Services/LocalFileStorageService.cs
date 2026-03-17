using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using IOCv2.Application.Extensions.ProjectResources;

namespace IOCv2.Infrastructure.Services
{
    /// <summary>
    /// Local implementation of IFileStorageService that stores files on the server file system.
    /// Uses a configured root storage path and base URL to build file paths and accessible URLs.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        // Root folder on disk where uploaded files are stored.
        private string _storagePath;

        // Base URL used to build file-accessible URLs (e.g. https://example.com/Uploads).
        private string _baseUrl;

        private readonly ILogger<LocalFileStorageService> _logger;

        private readonly IMessageService _messageService;

        /// <summary>
        /// Constructs the local file storage service.
        /// Reads storage path and base URL from IConfiguration, falling back to FileParams defaults.
        /// Ensures the root storage directory exists.
        /// </summary>
        public LocalFileStorageService(
            IConfiguration configuration,
            ILogger<LocalFileStorageService> logger, IMessageService messageService)
        {
            _storagePath = configuration[ProjectResourceParams.FileParams.ConfigurationStoragePathKey]
                ?? GetStoragePath();

            _baseUrl = configuration[ProjectResourceParams.FileParams.ConfigurationBaseUrl]
                ?? ProjectResourceParams.FileParams.BaseUrl;
            _logger = logger;
            _messageService = messageService;

            // Create root directory if not exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        /// <summary>
        /// Uploads a file to a subfolder under the configured storage path and returns a public file URL.
        /// </summary>
        /// <param name="file">The uploaded file to save.</param>
        /// <param name="folder">Relative folder under the storage root where the file will be saved.</param>
        /// <param name="fileName">Optional filename to use; if null a GUID-prefixed name is generated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Publicly accessible URL for the saved file.</returns>
        public async Task<string> UploadFileAsync(
            IFormFile file,
            string folder,
            string? fileName = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure target folder exists under storage root.
                var folderPath = Path.Combine(_storagePath, folder);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Generate unique file name when none provided to avoid collisions.
                var uniqueFileName = string.IsNullOrEmpty(fileName)
                    ? $"{Guid.NewGuid():N}_{file.FileName}"
                    : fileName;

                var filePath = Path.Combine(folderPath, uniqueFileName);

                // Persist the incoming IFormFile to disk.
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream, cancellationToken);

                // Build a relative URL path (use forward slashes for URLs).
                var relativePath = Path.Combine(folder, uniqueFileName).Replace('\\', '/');
                var fileUrl = $"{_baseUrl}/{relativePath}";

                // Log success. Note: message key used here comes from resources.
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadAutoSetFileTypeError), filePath);

                return fileUrl;
            }
            catch (Exception ex)
            {
                // Log error and rethrow to allow upstream handling.
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.File.FileUploadedError), file.FileName);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file identified by its public URL.
        /// </summary>
        /// <param name="fileUrl">Public URL previously returned by UploadFileAsync or GetFileUrl.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if file was deleted; false if file did not exist or an error occurred.</returns>
        public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);

                // Delete only if file exists.
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.File.FileDeletedSuccess), filePath);
                    return true;
                }

                _logger.LogWarning(_messageService.GetMessage(MessageKeys.File.FileNotFound), fileUrl);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.File.FileDeletedError), fileUrl);
                return false;
            }
        }

        /// <summary>
        /// Checks whether a file exists for a given public URL.
        /// </summary>
        /// <param name="fileUrl">Public URL of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if file exists, false otherwise or on error.</returns>
        public async Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);
                return await Task.Run(() => File.Exists(filePath), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.File.FileCheckExistsError), fileUrl);
                return false;
            }
        }

        /// <summary>
        /// Builds a public URL for a given relative file path.
        /// </summary>
        /// <param name="filePath">Path relative to the storage root (may include folders).</param>
        /// <returns>Public URL to access the file.</returns>
        public string GetFileUrl(string filePath)
        {
            var relativePath = filePath.Replace('\\', '/');
            return $"{_baseUrl}/{relativePath}";
        }

        /// <summary>
        /// Opens a read-only stream for a file given its public URL.
        /// Throws FileNotFoundException if the file does not exist.
        /// </summary>
        /// <param name="fileUrl">Public URL of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Read-only Stream to the file content.</returns>
        public async Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(_messageService.GetMessage(MessageKeys.File.FileNotFound, fileUrl), fileUrl);
                }

                // Use Task.Run to keep API asynchronous and avoid blocking caller threads for IO.
                return await Task.Run(() => File.OpenRead(filePath), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.File.FileGetStreamError), fileUrl);
                throw;
            }
        }

        /// <summary>
        /// Returns FileInfo for the file identified by its public URL, or null if not found.
        /// </summary>
        /// <param name="fileUrl">Public URL of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>FileInfo or null when file does not exist or on error.</returns>
        public async Task<FileInfo?> GetFileInfoAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetFilePathFromUrl(fileUrl);
                if (!File.Exists(filePath)) return null;

                var fileInfo = new FileInfo(filePath);
                return await Task.Run(() => fileInfo, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.File.FileGetError), fileUrl);
                return null;
            }
        }

        /// <summary>
        /// Converts a public file URL into an absolute file path on disk.
        /// Removes the base URL and any configured "/Uploads" segment, then maps into the storage root.
        /// </summary>
        /// <param name="fileUrl">Public URL that was returned by UploadFileAsync / GetFileUrl.</param>
        /// <returns>Full file system path to the file under the configured storage root.</returns>
        public string GetFilePathFromUrl(string fileUrl)
        {
            // Remove base URL and known URL segments to produce a relative path under the storage root.
            var relativePath = fileUrl.Replace(_baseUrl, "").Replace("/Uploads", "").TrimStart('/');
            return Path.Combine(_storagePath, relativePath);
        }

        private string GetStoragePath()
        {
            // D:\GIT\Ppp\Internship-OneConnect_IOC_v2.0_Backend\IOCv2.API
            string currentDir = Directory.GetCurrentDirectory();
            // D:\GIT\Ppp
            string projectRoot = "";
            try
            {
                projectRoot = Directory.GetParent(currentDir)!.Parent!.FullName;
            }
            catch
            {
                projectRoot = currentDir; // Fallback to current directory if parent retrieval fails
            }
            // D:\GIT\Ppp\Uploads
            return string.Format(ProjectResourceParams.FileParams.StoragePath, projectRoot);
        }
    }
}
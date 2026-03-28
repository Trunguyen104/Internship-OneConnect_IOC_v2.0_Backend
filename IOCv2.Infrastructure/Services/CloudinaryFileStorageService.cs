using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace IOCv2.Infrastructure.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudinaryFileStorageService> _logger;
    private readonly string _folderPrefix;

    public CloudinaryFileStorageService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<CloudinaryFileStorageService> logger)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary is selected but Cloudinary credentials are missing.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _folderPrefix = configuration["Cloudinary:FolderPrefix"]?.Trim('/') ?? "iocv2";
    }

    public async Task<string> UploadFileAsync(
        IFormFile file,
        string folder,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = string.IsNullOrWhiteSpace(fileName)
            ? $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}"
            : Path.GetFileName(fileName);

        var extension = Path.GetExtension(uniqueFileName);
        var publicIdWithoutExtension = Path.GetFileNameWithoutExtension(uniqueFileName);
        var folderPath = BuildCloudinaryFolder(folder);
        var publicId = $"{folderPath}/{publicIdWithoutExtension}";
        var resourceType = ResolveResourceType(file.ContentType, extension);

        await using var stream = file.OpenReadStream();

        UploadResult uploadResult;

        if (resourceType == "image")
        {
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(uniqueFileName, stream),
                PublicId = publicId,
                Overwrite = true,
                UniqueFilename = false,
                UseFilename = false
            };

            uploadResult = await _cloudinary.UploadAsync(imageParams);
        }
        else
        {
            var rawParams = new RawUploadParams
            {
                File = new FileDescription(uniqueFileName, stream),
                PublicId = publicId,
                Overwrite = true,
                UniqueFilename = false,
                UseFilename = false
            };

            uploadResult = await _cloudinary.UploadAsync(rawParams);
        }

        if (uploadResult.Error != null)
        {
            _logger.LogError("Cloudinary upload error: {Message}", uploadResult.Error.Message);
            throw new InvalidOperationException(uploadResult.Error.Message);
        }

        var url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Cloudinary upload succeeded but URL is empty.");
        }

        return url;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var (publicId, resourceType) = ExtractPublicIdAndResourceType(fileUrl);
        if (string.IsNullOrWhiteSpace(publicId))
        {
            _logger.LogWarning("Cannot extract Cloudinary public_id from url: {FileUrl}", fileUrl);
            return false;
        }

        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = resourceType,
            Invalidate = true
        };

        var result = await _cloudinary.DestroyAsync(deletionParams);
        return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
               || string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, fileUrl);
            using var response = await client.SendAsync(req, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return filePath;
        }

        var (resourceType, deliveryType, publicIdWithoutExtension, publicIdWithExtension) =
            ExtractResourceInfo(filePath);

        if (string.IsNullOrWhiteSpace(publicIdWithoutExtension) &&
            string.IsNullOrWhiteSpace(publicIdWithExtension))
        {
            return filePath;
        }

        var requiresSignedUrl = resourceType == ResourceType.Raw ||
                                !string.Equals(deliveryType, "upload", StringComparison.OrdinalIgnoreCase);
        if (!requiresSignedUrl)
        {
            return filePath;
        }

        var publicId = !string.IsNullOrWhiteSpace(publicIdWithExtension)
            ? publicIdWithExtension
            : publicIdWithoutExtension;
        var resourceTypeValue = resourceType == ResourceType.Raw ? "raw" : "image";

        // Use inline signed URL so browsers can preview PDFs/images in a new tab,
        // while still allowing users to click the browser's built-in download action.
        var signedUrl = _cloudinary.DownloadPrivate(publicId!, false, null, deliveryType, null, resourceTypeValue);
        return string.IsNullOrWhiteSpace(signedUrl) ? filePath : signedUrl;
    }

    public async Task<Stream> GetFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();

        var response = await client.GetAsync(fileUrl, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var signedUrls = TryBuildSignedDownloadUrls(fileUrl);
            foreach (var signedUrl in signedUrls)
            {
                response.Dispose();
                response = await client.GetAsync(signedUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync(cancellationToken);
                }
            }
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "Cloudinary download failed. Status: {StatusCode}, Url: {Url}, Body: {Body}",
            (int)response.StatusCode,
            fileUrl,
            body);
        response.EnsureSuccessStatusCode();
        throw new InvalidOperationException("Cloudinary download failed.");
    }

    public async Task<FileInfo?> GetFileInfoAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(System.Net.Http.HttpMethod.Head, fileUrl);
            using var response = await client.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public string GetFilePathFromUrl(string resourceUrl) => resourceUrl;

    public string GetDomainUrl() => string.Empty;

    private string BuildCloudinaryFolder(string folder)
    {
        var cleaned = folder.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return _folderPrefix;
        }
        return $"{_folderPrefix}/{cleaned}";
    }

    private static string ResolveResourceType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".svg" };
        return imageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ? "image" : "raw";
    }

    private static (string publicId, ResourceType resourceType) ExtractPublicIdAndResourceType(string fileUrl)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            return (string.Empty, ResourceType.Image);
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        var uploadIndex = Array.FindIndex(segments, s => string.Equals(s, "upload", StringComparison.OrdinalIgnoreCase));
        if (uploadIndex <= 0 || uploadIndex >= segments.Length - 1)
        {
            return (string.Empty, ResourceType.Image);
        }

        var resourceType = string.Equals(segments[uploadIndex - 1], "raw", StringComparison.OrdinalIgnoreCase)
            ? ResourceType.Raw
            : ResourceType.Image;

        var publicIdParts = segments.Skip(uploadIndex + 1).ToList();
        if (publicIdParts.Count == 0)
        {
            return (string.Empty, resourceType);
        }

        if (publicIdParts[0].StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
            publicIdParts[0].Length > 1 &&
            publicIdParts[0].Skip(1).All(char.IsDigit))
        {
            publicIdParts.RemoveAt(0);
        }

        if (publicIdParts.Count == 0)
        {
            return (string.Empty, resourceType);
        }

        var last = publicIdParts[^1];
        var lastWithoutExtension = Path.GetFileNameWithoutExtension(last);
        publicIdParts[^1] = lastWithoutExtension;

        return (string.Join('/', publicIdParts), resourceType);
    }

    private IEnumerable<string> TryBuildSignedDownloadUrls(string fileUrl)
    {
        var (resourceType, defaultDeliveryType, publicIdWithoutExtension, publicIdWithExtension) =
            ExtractResourceInfo(fileUrl);

        if (string.IsNullOrWhiteSpace(publicIdWithoutExtension) && string.IsNullOrWhiteSpace(publicIdWithExtension))
        {
            yield break;
        }

        var resourceTypeValue = resourceType == ResourceType.Raw ? "raw" : "image";
        var deliveryTypeCandidates = new[]
        {
            defaultDeliveryType,
            "upload",
            "authenticated",
            "private"
        }
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        var publicIdCandidates = new[]
        {
            publicIdWithoutExtension,
            publicIdWithExtension
        }
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

        foreach (var deliveryType in deliveryTypeCandidates)
        {
            foreach (var publicId in publicIdCandidates)
            {
                yield return _cloudinary.DownloadPrivate(publicId!, true, null, deliveryType, null, resourceTypeValue);
            }
        }
    }

    private static (ResourceType resourceType, string deliveryType, string publicIdWithoutExtension, string publicIdWithExtension)
        ExtractResourceInfo(string fileUrl)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            return (ResourceType.Image, "upload", string.Empty, string.Empty);
        }

        var path = uri.AbsolutePath;
        var resourceType = path.Contains("/raw/", StringComparison.OrdinalIgnoreCase)
            ? ResourceType.Raw
            : ResourceType.Image;

        var deliveryType = "upload";
        if (path.Contains("/authenticated/", StringComparison.OrdinalIgnoreCase))
        {
            deliveryType = "authenticated";
        }
        else if (path.Contains("/private/", StringComparison.OrdinalIgnoreCase))
        {
            deliveryType = "private";
        }

        var match = Regex.Match(path,
            @"/(?:image|video|raw)/(?:upload|authenticated|private)/(?:v\d+/)?(?<publicId>.+)$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return (resourceType, deliveryType, string.Empty, string.Empty);
        }

        var publicIdWithExtension = match.Groups["publicId"].Value.Trim('/');
        var publicIdWithoutExtension = publicIdWithExtension;

        var slashIndex = publicIdWithExtension.LastIndexOf('/');
        var lastPart = slashIndex >= 0
            ? publicIdWithExtension[(slashIndex + 1)..]
            : publicIdWithExtension;

        var lastPartWithoutExtension = Path.GetFileNameWithoutExtension(lastPart);
        if (!string.IsNullOrWhiteSpace(lastPartWithoutExtension) &&
            !string.Equals(lastPart, lastPartWithoutExtension, StringComparison.Ordinal))
        {
            publicIdWithoutExtension = slashIndex >= 0
                ? $"{publicIdWithExtension[..(slashIndex + 1)]}{lastPartWithoutExtension}"
                : lastPartWithoutExtension;
        }

        return (resourceType, deliveryType, publicIdWithoutExtension, publicIdWithExtension);
    }
}

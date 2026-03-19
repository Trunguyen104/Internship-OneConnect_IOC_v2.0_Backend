using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Uploads.Commands.UploadImage;

public class UploadImageHandler : IRequestHandler<UploadImageCommand, Result<string>>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<UploadImageHandler> _logger;

    public UploadImageHandler(IFileStorageService fileStorageService, ILogger<UploadImageHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start UploadImage: {FileName} to {Folder}", request.File.FileName, request.Folder);

        try
        {
            var url = await _fileStorageService.UploadFileAsync(request.File, request.Folder, null, cancellationToken);
            
            _logger.LogInformation("Upload success: {Url}", url);
            return Result<string>.Success(url);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Upload failure: {Message}", ex.Message);
            return Result<string>.Failure(ex.Message);
        }
    }
}

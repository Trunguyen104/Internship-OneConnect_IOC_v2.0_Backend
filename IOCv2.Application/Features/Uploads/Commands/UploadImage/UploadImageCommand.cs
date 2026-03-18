using IOCv2.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IOCv2.Application.Features.Uploads.Commands.UploadImage;

public record UploadImageCommand : IRequest<Result<string>>
{
    public IFormFile File { get; init; } = null!;
    public string Folder { get; init; } = "General";
}

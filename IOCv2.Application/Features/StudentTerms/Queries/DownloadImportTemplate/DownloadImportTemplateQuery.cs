using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public record DownloadImportTemplateQuery(Guid TermId) : IRequest<Result<DownloadImportTemplateResponse>>;

public class DownloadImportTemplateResponse
{
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

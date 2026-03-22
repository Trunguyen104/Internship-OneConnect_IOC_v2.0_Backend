using IOCv2.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsPreview;

public record ImportStudentsPreviewCommand : IRequest<Result<ImportStudentsPreviewResponse>>
{
    public Guid TermId { get; init; }
    public IFormFile File { get; init; } = null!;
}

public class ImportStudentsPreviewResponse
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public List<ImportPreviewRow> PreviewData { get; set; } = new();
}

public class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Major { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

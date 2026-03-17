using IOCv2.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudents;

public record ImportStudentsPreviewCommand : IRequest<Result<ImportStudentsPreviewResponse>>
{
    public Guid TermId { get; init; }
    public IFormFile File { get; init; } = null!;
}

public record ImportStudentsPreviewResponse
{
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public List<ImportPreviewRow> PreviewData { get; init; } = new();
}

public record ImportPreviewRow
{
    public int RowNumber { get; init; }
    public string? StudentCode { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? DateOfBirth { get; init; }
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
}

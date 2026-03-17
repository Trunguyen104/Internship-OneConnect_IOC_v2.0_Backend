using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudents;

public record ImportStudentsConfirmCommand : IRequest<Result<ImportStudentsConfirmResponse>>
{
    public Guid TermId { get; init; }
    public List<ImportValidRecord> ValidRecords { get; init; } = new();
}

public record ImportValidRecord
{
    public string StudentCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? DateOfBirth { get; init; }
    public string? Major { get; init; }
}

public record ImportStudentsConfirmResponse
{
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }

    /// <summary>
    /// Base64-encoded Excel file containing MSSV, Họ tên, Email, Password for new accounts.
    /// BE generates in-memory and returns here; not persisted to disk.
    /// </summary>
    public string? PasswordFileBase64 { get; init; }
    public string? PasswordFileFileName { get; init; }
}

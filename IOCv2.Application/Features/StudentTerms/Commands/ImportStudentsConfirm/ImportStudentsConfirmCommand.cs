using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsConfirm;

public record ImportStudentsConfirmCommand : IRequest<Result<ImportStudentsConfirmResponse>>
{
    public Guid TermId { get; init; }
    public List<ImportStudentRecord> ValidRecords { get; init; } = new();
}

public class ImportStudentRecord
{
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Major { get; set; }
}

public class ImportStudentsConfirmResponse
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? PasswordFileBase64 { get; set; }
    public string? PasswordFileFileName { get; set; }
}

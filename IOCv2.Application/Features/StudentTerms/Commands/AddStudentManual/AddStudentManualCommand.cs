using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;

public record AddStudentManualCommand : IRequest<Result<AddStudentManualResponse>>
{
    public Guid TermId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string StudentCode { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Major { get; init; }
}

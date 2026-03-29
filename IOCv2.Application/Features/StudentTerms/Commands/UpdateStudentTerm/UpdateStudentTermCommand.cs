using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public record UpdateStudentTermCommand : IRequest<Result<UpdateStudentTermResponse>>
{
    public Guid StudentTermId { get; init; }
    public string? StudentCode { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Major { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public DateOnly? EnrollmentDate { get; init; }
    public EnrollmentStatus? EnrollmentStatus { get; init; }
    public string? EnrollmentNote { get; init; }
    public PlacementStatus? PlacementStatus { get; init; }
    public Guid? EnterpriseId { get; init; }
}

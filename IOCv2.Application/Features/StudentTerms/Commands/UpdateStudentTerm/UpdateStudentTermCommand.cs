using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public record UpdateStudentTermCommand : IRequest<Result<UpdateStudentTermResponse>>
{
    public Guid StudentTermId { get; init; }

    // Student profile fields (also update users/students tables)
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Major { get; init; }
    public DateOnly? DateOfBirth { get; init; }

    // Enrollment fields
    public DateOnly? EnrollmentDate { get; init; }
    public EnrollmentStatus? EnrollmentStatus { get; init; }
    public string? EnrollmentNote { get; init; }

    // Placement fields
    public PlacementStatus? PlacementStatus { get; init; }
    public Guid? EnterpriseId { get; init; }
}

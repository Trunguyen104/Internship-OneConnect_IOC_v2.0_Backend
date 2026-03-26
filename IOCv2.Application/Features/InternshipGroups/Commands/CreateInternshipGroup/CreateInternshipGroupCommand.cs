using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    /// <summary>
    /// Command to create a new internship group.
    /// </summary>
    public record CreateInternshipGroupCommand : IRequest<Result<CreateInternshipGroupResponse>>
    {
        /// <summary>
        /// Identity of the internship phase.
        /// </summary>
        public Guid PhaseId { get; init; }

        /// <summary>
        /// Display name of the internship group.
        /// </summary>
        public string GroupName { get; init; } = string.Empty;

        /// <summary>
        /// Optional description of the internship group.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Optional identity of the associated enterprise.
        /// </summary>
        public Guid? EnterpriseId { get; init; }

        /// <summary>
        /// Optional identity of the mentor in charge of the group.
        /// </summary>
        public Guid? MentorId { get; init; }

        /// <summary>
        /// List of initial students to be added to the group.
        /// </summary>
        public List<CreateInternshipStudentDto> Students { get; init; } = new List<CreateInternshipStudentDto>();
    }

    /// <summary>
    /// DTO for initial student assignment with role.
    /// </summary>
    public record CreateInternshipStudentDto
    {
        /// <summary>
        /// Identity of the student.
        /// </summary>
        public Guid StudentId { get; init; }

        /// <summary>
        /// Assigned role for the student in the group (e.g., Leader, Member).
        /// </summary>
        public InternshipRole Role { get; init; }
    }
}


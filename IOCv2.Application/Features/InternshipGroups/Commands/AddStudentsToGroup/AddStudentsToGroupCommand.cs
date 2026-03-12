using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    /// <summary>
    /// Command to add multiple students to an internship group.
    /// </summary>
    public record AddStudentsToGroupCommand : IRequest<Result<AddStudentsToGroupResponse>>
    {
        /// <summary>
        /// Unique identifier of the internship group.
        /// </summary>
        public Guid InternshipId { get; init; }

        /// <summary>
        /// List of students and their assigned roles to add to the group.
        /// </summary>
        public List<AddStudentItemDto> Students { get; init; } = new List<AddStudentItemDto>();
    }

    /// <summary>
    /// Student entry for group addition.
    /// </summary>
    public record AddStudentItemDto
    {
        /// <summary>
        /// Unique identifier of the student.
        /// </summary>
        public Guid StudentId { get; init; }

        /// <summary>
        /// Assigned role for the student in the group.
        /// </summary>
        public InternshipRole Role { get; init; }
    }
}


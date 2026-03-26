using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    /// <summary>
    /// Command to update an existing internship group.
    /// </summary>
    public record UpdateInternshipGroupCommand : IRequest<Result<UpdateInternshipGroupResponse>>
    {
        /// <summary>
        /// Unique identifier of the internship group to update.
        /// </summary>
        [JsonIgnore]
        public Guid InternshipId { get; init; }

        /// <summary>
        /// Identity of the internship phase.
        /// </summary>
        public Guid PhaseId { get; init; }

        /// <summary>
        /// Updated name of the internship group.
        /// </summary>
        public string GroupName { get; init; } = string.Empty;

        /// <summary>
        /// Updated description.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Updated enterprise identity.
        /// </summary>
        public Guid? EnterpriseId { get; init; }

        /// <summary>
        /// Updated mentor identity.
        /// </summary>
        public Guid? MentorId { get; init; }


    }
}

using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    /// <summary>
    /// Command to remove multiple students from an internship group.
    /// </summary>
    public record RemoveStudentsFromGroupCommand : IRequest<Result<RemoveStudentsFromGroupResponse>>
    {
        /// <summary>
        /// Unique identifier of the internship group.
        /// </summary>
        [JsonIgnore]
        public Guid InternshipId { get; init; }

        /// <summary>
        /// List of student identifiers to remove from the group.
        /// </summary>
        public List<Guid> StudentIds { get; init; } = new List<Guid>();
    }
}

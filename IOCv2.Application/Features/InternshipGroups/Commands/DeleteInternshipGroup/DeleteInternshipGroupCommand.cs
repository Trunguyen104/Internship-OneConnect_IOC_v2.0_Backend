using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    /// <summary>
    /// Command to delete an internship group and its members.
    /// </summary>
    public record DeleteInternshipGroupCommand : IRequest<Result<DeleteInternshipGroupResponse>>
    {
        /// <summary>
        /// Unique identifier of the internship group to delete.
        /// </summary>
        public Guid InternshipId { get; init; }

    }
}

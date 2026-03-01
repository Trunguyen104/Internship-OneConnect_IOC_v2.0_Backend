using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public record RemoveStudentsFromGroupCommand : IRequest<Result<RemoveStudentsFromGroupResponse>>
    {
        [JsonIgnore]
        public Guid InternshipId { get; init; }

        public List<Guid> StudentIds { get; init; } = new List<Guid>();
    }
}

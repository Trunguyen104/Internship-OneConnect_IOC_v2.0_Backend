using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public class RemoveStudentsFromGroupCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore]
        public Guid InternshipId { get; set; }

        public List<Guid> StudentIds { get; set; } = new List<Guid>();
    }
}

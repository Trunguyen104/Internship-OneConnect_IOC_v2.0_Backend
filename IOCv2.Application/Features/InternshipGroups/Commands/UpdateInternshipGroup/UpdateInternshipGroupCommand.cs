using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    public record UpdateInternshipGroupCommand : IRequest<Result<UpdateInternshipGroupResponse>>
    {
        [JsonIgnore]
        public Guid InternshipId { get; init; }

        public Guid TermId { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public Guid? EnterpriseId { get; init; }
        public Guid? MentorId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }
}

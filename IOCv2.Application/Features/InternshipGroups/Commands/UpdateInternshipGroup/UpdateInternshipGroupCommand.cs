using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    public class UpdateInternshipGroupCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore]
        public Guid InternshipId { get; set; }

        public Guid TermId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid? EnterpriseId { get; set; }
        public Guid? MentorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}

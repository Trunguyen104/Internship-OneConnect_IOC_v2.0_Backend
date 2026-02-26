using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore]
        public Guid InternshipId { get; set; }

        public List<AddStudentItemDto> Students { get; set; } = new List<AddStudentItemDto>();
    }

    public class AddStudentItemDto
    {
        public Guid StudentId { get; set; }
        public InternshipRole Role { get; set; }
    }
}

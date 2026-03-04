using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public record AddStudentsToGroupCommand : IRequest<Result<AddStudentsToGroupResponse>>
    {
        [JsonIgnore]
        public Guid InternshipId { get; init; }

        public List<AddStudentItemDto> Students { get; init; } = new List<AddStudentItemDto>();
    }

    public record AddStudentItemDto
    {
        public Guid StudentId { get; init; }
        public string Role { get; init; } = string.Empty;
    }
}

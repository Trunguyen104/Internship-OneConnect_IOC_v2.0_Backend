using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    public record CreateInternshipGroupCommand : IRequest<Result<CreateInternshipGroupResponse>>
    {
        public Guid TermId { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public Guid? EnterpriseId { get; init; }
        public Guid? MentorId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }

        public List<CreateInternshipStudentDto> Students { get; init; } = new List<CreateInternshipStudentDto>();
    }

    public record CreateInternshipStudentDto
    {
        public Guid StudentId { get; init; }
        public InternshipRole Role { get; init; }
    }
}

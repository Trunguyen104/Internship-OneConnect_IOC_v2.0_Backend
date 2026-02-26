using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    public class CreateInternshipGroupCommand : IRequest<Result<Guid>>
    {
        public Guid TermId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid? EnterpriseId { get; set; }
        public Guid? MentorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<CreateInternshipStudentDto> Students { get; set; } = new List<CreateInternshipStudentDto>();
    }

    public class CreateInternshipStudentDto
    {
        public Guid StudentId { get; set; }
        public InternshipRole Role { get; set; }
    }
}

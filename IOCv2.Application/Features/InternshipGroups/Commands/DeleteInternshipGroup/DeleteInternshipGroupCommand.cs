using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupCommand : IRequest<Result<Guid>>
    {
        public Guid InternshipId { get; set; }

        public DeleteInternshipGroupCommand(Guid internshipId)
        {
            InternshipId = internshipId;
        }
    }
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public record DeleteInternshipGroupCommand : IRequest<Result<DeleteInternshipGroupResponse>>
    {
        public Guid InternshipId { get; init; }

        public DeleteInternshipGroupCommand(Guid internshipId)
        {
            InternshipId = internshipId;
        }
    }
}

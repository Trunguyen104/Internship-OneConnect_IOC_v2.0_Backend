using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.AssignGroup
{
    public class AssignGroupCommand : IRequest<Result<AssignGroupResponse>>
    {
        public Guid ProjectId { get; set; }
        public Guid InternshipId { get; set; }
    }
}

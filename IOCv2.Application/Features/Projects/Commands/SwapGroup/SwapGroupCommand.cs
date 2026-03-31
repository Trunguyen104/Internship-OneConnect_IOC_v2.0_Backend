using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.SwapGroup
{
    public class SwapGroupCommand : IRequest<Result<SwapGroupResponse>>
    {
        public Guid ProjectId { get; set; }
        public Guid NewInternshipId { get; set; }
        public Guid? ReplacementProjectId { get; set; }
    }
}

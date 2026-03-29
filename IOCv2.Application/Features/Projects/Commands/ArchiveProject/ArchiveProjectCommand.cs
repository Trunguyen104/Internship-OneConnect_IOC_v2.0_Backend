using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.ArchiveProject
{
    public class ArchiveProjectCommand : IRequest<Result<ArchiveProjectResponse>>
    {
        public Guid ProjectId { get; set; }
    }
}

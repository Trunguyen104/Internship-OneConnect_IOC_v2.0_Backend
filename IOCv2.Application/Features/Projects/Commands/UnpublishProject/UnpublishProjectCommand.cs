using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.UnpublishProject
{
    public class UnpublishProjectCommand : IRequest<Result<UnpublishProjectResponse>>
    {
        public Guid ProjectId { get; set; }
    }
}

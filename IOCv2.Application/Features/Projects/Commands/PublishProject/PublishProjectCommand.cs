using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.PublishProject
{
    public class PublishProjectCommand : IRequest<Result<PublishProjectResponse>>
    {
        public Guid ProjectId { get; set; }
    }
}

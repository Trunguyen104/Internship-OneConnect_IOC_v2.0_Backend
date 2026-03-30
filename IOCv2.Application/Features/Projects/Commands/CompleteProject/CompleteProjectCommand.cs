using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.CompleteProject
{
    public class CompleteProjectCommand : IRequest<Result<CompleteProjectResponse>>
    {
        public Guid ProjectId { get; set; }
    }
}

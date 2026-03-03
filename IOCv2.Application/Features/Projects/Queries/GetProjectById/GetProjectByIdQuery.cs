using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class GetProjectByIdQuery : IRequest<Result<GetProjectByIdResponse>>
    {
        public Guid ProjectId { get; set; }
    }
}

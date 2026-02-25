using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectResourceById
{
    public record GetProjectResourceByIdQuery : IRequest<Result<GetProjectsByStudentIdResponse>>
    {
        public Guid ProjectResourceId { get; set; }
    }
}

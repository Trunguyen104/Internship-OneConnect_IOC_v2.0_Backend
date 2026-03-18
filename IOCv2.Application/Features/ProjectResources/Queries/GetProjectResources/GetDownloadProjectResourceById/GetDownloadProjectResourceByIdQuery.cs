using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById
{
    public record GetDownloadProjectResourceByIdQuery : IRequest<Result<GetDownloadProjectResourceByIdResponse>>
    {
        public Guid ProjectResourceId { get; set; }
    }
}
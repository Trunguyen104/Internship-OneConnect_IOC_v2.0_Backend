using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById
{
    public class GetDownloadProjectResourceByIdResponse
    {
        public FileStreamResult FileResponse { get; set; }
    }
}
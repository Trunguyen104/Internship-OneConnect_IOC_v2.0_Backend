using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetDownloadProjectResourceById
{
    /// <summary>
    /// Response returned when requesting a downloadable project resource by id.
    /// Contains the FileStreamResult to be returned by the controller/action layer.
    /// </summary>
    public class GetDownloadProjectResourceByIdResponse
    {
        /// <summary>
        /// The file stream result representing the downloadable file.
        /// May be null if an error occurred before creating the FileStreamResult.
        /// </summary>
        public FileStreamResult? FileResponse { get; set; }
    }
}
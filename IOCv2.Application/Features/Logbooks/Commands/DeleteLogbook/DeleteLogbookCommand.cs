using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    /// <summary>
    /// Command to soft-delete a logbook entry.
    /// </summary>
    public record DeleteLogbookCommand : IRequest<Result<DeleteLogbookResponse>>
    {
        /// <summary>
        /// Internship group ID from route.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// ID of the logbook to delete.
        /// </summary>
        public Guid LogbookId { get; set; }
    }
}

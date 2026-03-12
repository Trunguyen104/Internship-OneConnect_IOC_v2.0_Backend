using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook
{
    /// <summary>
    /// Command to update an existing logbook entry.
    /// </summary>
    public record UpdateLogbookCommand : IRequest<Result<UpdateLogbookResponse>>
    {
        /// <summary>
        /// ID of the logbook to update.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid LogbookId { get; set; }
 
         /// <summary>
         /// Internship group ID from route.
         /// </summary>
         public Guid InternshipId { get; set; }



        /// <summary>
        /// Updated summary of activities.
        /// </summary>
        public required string Summary { get; set; }

        /// <summary>
        /// Updated issues description.
        /// </summary>
        public string? Issue { get; set; }

        /// <summary>
        /// Updated plans for the next period.
        /// </summary>
        public required string Plan { get; set; }

        /// <summary>
        /// Updated date report covers.
        /// </summary>
        public DateTime DateReport { get; set; }

        /// <summary>
        /// Status of the logbook.
        /// </summary>
        public LogbookStatus? Status { get; set; }
    }
}

using IOCv2.Application.Common.Models;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IOCv2.Application.Extensions.Mappings;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    /// <summary>
    /// Command to create a new logbook entry.
    /// </summary>
    public record CreateLogbookCommand : IRequest<Result<CreateLogbookResponse>>, IMapFrom<Logbook>
    {
        /// <summary>
        /// ID of the internship group the logbook belongs to.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Summary of activities performed.
        /// </summary>
        public required string Summary { get; set; }

        /// <summary>
        /// Description of any issues encountered.
        /// </summary>
        public string? Issue { get; set; }

        /// <summary>
        /// Plans for the next period.
        /// </summary>
        public required string Plan { get; set; }

        /// <summary>
        /// Date the report covers.
        /// </summary>
        public DateTime DateReport { get; set; }

        /// <summary>
        /// Initial status of the logbook.
        /// </summary>
        public LogbookStatus? Status { get; set; }
    }
}

using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbookById
{
    /// <summary>
    /// Query to get detailed information of a logbook by its ID.
    /// </summary>
    public record GetLogbookByIdQuery : IRequest<Result<GetLogbookByIdResponse>>
    {
        /// <summary>
        /// ID of the logbook to retrieve.
        /// </summary>
        public Guid LogbookId { get; init; }
    }
}

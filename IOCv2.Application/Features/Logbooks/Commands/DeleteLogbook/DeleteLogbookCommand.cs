using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    /// <summary>
    /// Command to soft-delete a logbook entry.
    /// </summary>
    public record DeleteLogbookCommand : IRequest<Result<DeleteLogbookResponse>>
    {
        /// <summary>
        /// ID of the logbook to delete.
        /// </summary>
        public Guid LogbookId { get; set; }
    }
}

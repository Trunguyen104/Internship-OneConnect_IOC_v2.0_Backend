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
    public record UpdateLogbookCommand : IRequest<Result<UpdateLogbookResponse>>
    {
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }
        public Guid StudentId { get; set; }
        public required string Summary { get; set; }
        public string? Issue { get; set; }
        public required string Plan { get; set; }
        public DateTime DateReport { get; set; }
        public LogbookStatus Status { get; set; }
    }
}

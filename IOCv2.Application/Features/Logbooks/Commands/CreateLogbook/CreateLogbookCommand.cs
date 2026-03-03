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
    public record CreateLogbookCommand : IRequest<Result<CreateLogbookResponse>>, IMapFrom<Logbook>
    {
        public Guid ProjectId { get; set; }
        public required string Summary { get; set; }
        public string? Issue { get; set; }
        public required string Plan { get; set; }
        public DateTime DateReport { get; set; }
        public LogbookStatus Status { get; set; }
    }
}

using IOCv2.Application.Common.Models;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookCommand : IRequest<Result<CreateLogbookResponse>>
    {
        public Guid InternshipId { get; set; }
        public Guid StudentId { get; set; }
        public required string Content { get; set; }
        public string? Issue { get; set; }
        public DateTime DateReport { get; set; }
        public LogbookStatus Status { get; set; }
    }
}

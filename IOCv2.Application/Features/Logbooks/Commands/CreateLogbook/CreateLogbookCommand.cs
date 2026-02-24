using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookCommand
    {
        public int InternshipId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public LogbookStatus Status { get; set; }
    }
}

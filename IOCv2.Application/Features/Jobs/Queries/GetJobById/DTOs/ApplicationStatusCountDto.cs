using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById.DTOs
{
    public class ApplicationStatusCountDto
    {
        // JobApplicationStatus as short (matches enum values)
        public short Status { get; set; }
        public string? StatusName { get; set; }
        public int Count { get; set; }
    }
}

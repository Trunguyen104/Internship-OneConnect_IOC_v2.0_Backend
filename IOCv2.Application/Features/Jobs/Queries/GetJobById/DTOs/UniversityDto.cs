using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById.DTOs
{
    public class UniversityDto
    {
        public Guid UniversityId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

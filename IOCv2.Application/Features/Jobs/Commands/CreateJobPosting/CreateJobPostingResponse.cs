using IOCv2.Application.Extensions.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public class CreateJobPostingResponse : IMapFrom<Domain.Entities.Job>
    {
        public Guid JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Requirements { get; set; }
        public string? Benefit { get; set; }
        public string? Location { get; set; }
        public int? Quantity { get; set; }
        public DateTime? ExpireDate { get; private set; }
    }
}

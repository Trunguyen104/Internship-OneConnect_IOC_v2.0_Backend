using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public class UpdateJobApplicationStatusResponse
    {
        public Guid ApplicationId { get; set; }
        public short Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

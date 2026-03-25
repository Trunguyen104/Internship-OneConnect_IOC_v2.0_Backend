
using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public record CreateJobPostingCommand : IRequest<Result<CreateJobPostingResponse>>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Benefit { get; set; }
        public string? Location { get; set; }
        public int? Quantity { get; set; }
        public DateTime? ExpireDate { get; private set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using IOCv2.Application.Common.Models;

namespace IOCv2.Application.Features.Jobs.Commands.ReopenJob
{
    public record ReopenJobPostingCommand : IRequest<Result<ReopenJobPostingResponse>>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }
        public DateTime ExpireDate { get; init; }
    }
}

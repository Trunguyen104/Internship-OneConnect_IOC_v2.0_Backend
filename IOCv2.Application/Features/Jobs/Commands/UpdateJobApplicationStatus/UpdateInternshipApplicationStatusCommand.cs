using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public record UpdateInternshipApplicationStatusCommand : IRequest<Result<UpdateInternshipApplicationStatusResponse>>
    {
        [JsonIgnore]
        public Guid ApplicationId { get; init; }

        public InternshipApplicationStatus NewStatus { get; init; }

        /// <summary>
        /// Required when NewStatus == Rejected.
        /// Optional otherwise.
        /// </summary>
        public string? RejectReason { get; init; }
    }    
}
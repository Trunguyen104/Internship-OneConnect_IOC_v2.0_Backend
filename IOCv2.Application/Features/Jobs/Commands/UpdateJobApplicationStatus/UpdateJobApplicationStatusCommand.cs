using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public record UpdateJobApplicationStatusCommand : IRequest<Result<UpdateJobApplicationStatusResponse>>
    {
        [JsonIgnore]
        public Guid ApplicationId { get; init; }
        public JobApplicationStatus NewStatus { get; set; }
        public DateTime? InterviewTime { get; set; } // optional scheduling info
        public string? Note { get; set; }
    }    
}
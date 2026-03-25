using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    public record UpdateJobCommand : IRequest<Result<UpdateJobResponse>>, IMapFrom<Job>
    {
        [JsonIgnore]
        public Guid JobId { get; set; }

        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Requirements { get; init; }
        public string? Location { get; init; }
        public int? Quantity { get; init; }
        public DateTime? ExpireDate { get; init; }

        /// <summary>
        /// If the job already has any applications and this flag is true, proceed with update.
        /// If false, handler will return a warning and not apply changes.
        /// </summary>
        public bool ConfirmWhenHasApplications { get; init; } = false;
    }
}

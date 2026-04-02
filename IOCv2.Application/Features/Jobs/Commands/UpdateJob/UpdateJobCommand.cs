using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    public record UpdateJobCommand : IRequest<Result<UpdateJobResponse>>, IMapFrom<Job>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }
        public string? Title { get; init; } = string.Empty;
        public string? Position { get; init; }
        public string? Description { get; init; }
        public string? Requirements { get; init; }
        public string? Benefit { get; init; }
        public string? Location { get; init; }
        public JobStatus Status { get; init; }
        public DateTime? ExpireDate { get; init; }
        public JobAudience Audience { get; init; }

        /// <summary>
        /// Selected internship phase for this job posting (nullable).
        /// AC-05 requires special handling when changing this for Published jobs with active applications.
        /// </summary>
        public Guid? InternshipPhaseId { get; init; }

        /// <summary>
        /// When Audience == Targeted this should contain the single target university id.
        /// For Public audience this can be null/empty.
        /// </summary>
        public List<Guid>? UniversityIds { get; init; }

        /// <summary>
        /// When updating a published job which already has applications, the frontend must prompt the HR
        /// and then call update again with <see cref="ForceUpdateWithApplications"/> = true to proceed.
        /// </summary>
        public bool ForceUpdateWithApplications { get; init; }
    }
}

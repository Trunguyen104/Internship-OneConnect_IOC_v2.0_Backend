using MediatR;
using IOCv2.Domain.Enums;
using System;
using IOCv2.Application.Common.Models;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobDraft
{
    public record CreateJobDraftCommand : IRequest<Result<CreateJobDraftResponse>>
    {
        // Minimal fields supported for draft (Title is required for auto-save)
        public string Title { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Benefit { get; set; }
        public string? Location { get; set; }
        public int? Quantity { get; set; }
        public DateTime? ExpireDate { get; set; }

        // Optional for draft; required when publishing/creating final posting.
        public Guid? InternshipPhaseId { get; set; }

        // Internship period optional for draft (can be provided)
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Audience: optional, defaults to Public
        public JobAudience Audience { get; set; } = JobAudience.Public;

        // Optional when Audience == Targeted
        public Guid? UniversityId { get; set; }
    }
}

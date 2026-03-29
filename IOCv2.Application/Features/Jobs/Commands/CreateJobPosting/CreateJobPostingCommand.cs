using IOCv2.Application.Common.Models;
using MediatR;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public record CreateJobPostingCommand : IRequest<Result<CreateJobPostingResponse>>
    {
        public string Title { get; init; } = string.Empty;
        public string? Position { get; init; }
        public string? Description { get; init; }
        public string? Requirements { get; init; }
        public string? Benefit { get; init; }
        public string? Location { get; init; }
        public int? Quantity { get; init; }
        public DateTime? ExpireDate { get; init; }

        // New required internship period fields
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }

        // Audience: Public or Targeted
        public JobAudience Audience { get; init; } = JobAudience.Public;

        // Required when Audience == Targeted
        public Guid? UniversityId { get; init; }
    }
}

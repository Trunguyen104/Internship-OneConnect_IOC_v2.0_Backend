using IOCv2.Application.Common.Models;
using MediatR;
using System;

namespace IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup
{
    public record ArchiveInternshipGroupCommand : IRequest<Result<ArchiveInternshipGroupResponse>>
    {
        public Guid InternshipGroupId { get; init; }
    }
}

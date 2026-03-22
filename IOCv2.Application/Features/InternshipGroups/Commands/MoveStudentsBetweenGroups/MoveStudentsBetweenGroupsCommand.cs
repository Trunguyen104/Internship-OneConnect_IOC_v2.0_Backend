using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.InternshipGroups.Commands.MoveStudentsBetweenGroups
{
    public record MoveStudentsBetweenGroupsCommand : IRequest<Result<MoveStudentsBetweenGroupsResponse>>
    {
        public List<Guid> StudentIds { get; init; } = new();
        public Guid FromGroupId { get; init; }
        public Guid ToGroupId { get; init; }
    }
}

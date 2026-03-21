using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.InternshipGroups.Commands.MoveStudentsBetweenGroups
{
    public class MoveStudentsBetweenGroupsResponse
    {
        public List<Guid> StudentIds { get; set; } = new();
        public Guid FromGroupId { get; set; }
        public Guid ToGroupId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

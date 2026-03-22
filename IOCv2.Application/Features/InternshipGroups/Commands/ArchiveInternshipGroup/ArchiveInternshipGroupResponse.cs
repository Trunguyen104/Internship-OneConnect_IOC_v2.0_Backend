using System;

namespace IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup
{
    public class ArchiveInternshipGroupResponse
    {
        public Guid InternshipGroupId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

using System;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    public class GetPlacedStudentsResponse
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public bool IsAssignedToGroup { get; set; }
        public Guid? AssignedGroupId { get; set; }
        public string? AssignedGroupName { get; set; }
    }
}

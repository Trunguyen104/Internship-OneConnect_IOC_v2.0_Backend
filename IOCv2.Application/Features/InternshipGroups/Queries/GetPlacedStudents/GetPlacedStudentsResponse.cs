using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    public class GetPlacedStudentsResponse
    {
        // ── Thông tin sinh viên ────────────────────────────────────────────────
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string? UniversityName { get; set; }

        // ── Thông tin nhóm ────────────────────────────────────────────────────
        public bool IsAssignedToGroup { get; set; }
        public Guid? AssignedGroupId { get; set; }
        public string? AssignedGroupName { get; set; }
        public string? MentorName { get; set; }      // Tên mentor của nhóm đã được phân công

        // ── Thông tin kỳ thực tập ─────────────────────────────────────────────
        public Guid PhaseId { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public string PhaseStatus { get; set; } = string.Empty;  // "Upcoming" | "Active" | "Ended" | "Closed"
        public DateOnly PhaseStartDate { get; set; }
        public DateOnly PhaseEndDate { get; set; }
    }
}
